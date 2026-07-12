using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class CatalogFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly CatalogService _catalog;

    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }

    public CatalogFlowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options, _tenant);
        _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _catalog = new CatalogService(_db, _tenant);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task Seeder_creates_required_catalog_volume()
    {
        Assert.Equal(12, await _db.Categories.CountAsync(c => c.ParentId == null));
        Assert.Equal(40, await _db.Categories.CountAsync(c => c.ParentId != null));
        Assert.Equal(10, await _db.Brands.CountAsync());
        Assert.Equal(250, await _db.Products.CountAsync());
        Assert.True(await _db.ProductVariants.CountAsync() >= 100);
        Assert.True(await _db.QuantityPriceTiers.CountAsync() >= 750);
    }

    [Fact]
    public async Task Product_search_filters_and_paginates()
    {
        var category = await _db.Categories.FirstAsync(c => c.ParentId == null);
        var result = await _catalog.ProductsAsync(new ProductQuery
        {
            CategoryId = category.Id,
            Page = 1,
            PageSize = 7,
            Sort = "price_asc",
        }, null);
        Assert.Equal(7, result.Items.Count);
        Assert.True(result.Total > 7);
        Assert.True(result.Items.Zip(result.Items.Skip(1)).All(pair => pair.First.Price <= pair.Second.Price));

        var searched = await _catalog.ProductsAsync(new ProductQuery { Q = result.Items[0].Sku }, null);
        Assert.Single(searched.Items);
    }

    [Fact]
    public async Task Detail_contains_tiers_attributes_variants_and_related_products()
    {
        var product = await _db.Products.FirstAsync(p => p.Variants.Count != 0);
        var detail = await _catalog.ProductAsync(product.Slug, null);
        Assert.True(detail.PriceTiers.Count >= 3);
        Assert.True(detail.Attributes.Count >= 3);
        Assert.True(detail.Variants.Count >= 2);
        Assert.NotEmpty(detail.RelatedProducts);
    }

    [Fact]
    public async Task Contract_price_and_favorite_are_tenant_scoped()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _tenant.TenantId = tenantId;
        var product = await _db.Products.FirstAsync();
        _db.CompanyProductPrices.Add(new CompanyProductPrice
        {
            TenantId = tenantId,
            ProductId = product.Id,
            ContractPrice = 7.50m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
        });
        await _db.SaveChangesAsync();

        var result = await _catalog.ProductsAsync(new ProductQuery { Q = product.Sku }, userId);
        Assert.Equal(7.50m, Assert.Single(result.Items).Price);
        Assert.True(result.Items[0].HasContractPrice);

        Assert.True(await _catalog.ToggleFavoriteAsync(product.Id, userId));
        result = await _catalog.ProductsAsync(new ProductQuery { Q = product.Sku }, userId);
        Assert.True(Assert.Single(result.Items).IsFavorite);
        Assert.False(await _catalog.ToggleFavoriteAsync(product.Id, userId));
    }
}
