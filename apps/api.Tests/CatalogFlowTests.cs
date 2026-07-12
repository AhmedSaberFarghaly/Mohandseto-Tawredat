using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Controllers;
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

    [Fact]
    public async Task Compare_and_recent_searches_are_limited_and_tenant_scoped()
    {
        _tenant.TenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var products = await _db.Products.OrderBy(p => p.Sku).Take(5).ToListAsync();

        await _catalog.ProductsAsync(new ProductQuery { Q = products[0].Sku }, userId);
        Assert.Contains(products[0].Sku, await _catalog.RecentSearchesAsync(userId));

        foreach (var product in products.Take(4))
            Assert.True(await _catalog.ToggleCompareAsync(product.Id, userId));
        Assert.Equal(4, (await _catalog.CompareAsync(userId)).Count);
        var error = await Assert.ThrowsAsync<Mohandseto.Api.Application.Common.ApiException>(
            () => _catalog.ToggleCompareAsync(products[4].Id, userId));
        Assert.Equal(400, error.StatusCode);

        await _catalog.ClearCompareAsync(userId);
        Assert.Empty(await _catalog.CompareAsync(userId));
        await _catalog.ClearRecentSearchesAsync(userId);
        Assert.Empty(await _catalog.RecentSearchesAsync(userId));
    }

    [Fact]
    public async Task Admin_can_replace_product_variants()
    {
        var product = await _db.Products.FirstAsync();
        var controller = new AdminCatalogController(_db, _catalog);
        var variants = new[]
        {
            new UpsertVariantDto(null, $"{product.Sku}-BLUE", "أزرق", "Blue", "{\"color\":\"blue\"}", 12.5m, 7, true),
            new UpsertVariantDto(null, $"{product.Sku}-RED", "أحمر", "Red", "{\"color\":\"red\"}", 9m, 4, true),
        };

        Assert.IsType<OkObjectResult>(await controller.ReplaceVariants(product.Id, variants, default));
        var stored = await _db.ProductVariants.Where(v => v.ProductId == product.Id).OrderBy(v => v.Sku).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Equal(12.5m, stored[0].PriceAdjustment);
        Assert.Equal(11, stored.Sum(v => v.StockQty));
    }

    [Fact]
    public async Task Admin_export_can_be_imported_as_an_update_roundtrip()
    {
        var controller = new AdminCatalogController(_db, _catalog);
        var export = Assert.IsType<FileContentResult>(await controller.ExportProducts(null, default));
        Assert.Equal("text/csv; charset=utf-8", export.ContentType);
        Assert.Contains("categorySlug", System.Text.Encoding.UTF8.GetString(export.FileContents));

        await using var stream = new MemoryStream(export.FileContents);
        IFormFile upload = new FormFile(stream, 0, stream.Length, "file", "products.csv")
        {
            Headers = new HeaderDictionary(), ContentType = "text/csv",
        };
        Assert.IsType<OkObjectResult>(await controller.ImportProducts(upload, default));
        Assert.Equal(250, await _db.Products.CountAsync());
    }

    [Fact]
    public async Task Admin_can_create_update_and_archive_brand()
    {
        var controller = new AdminCatalogController(_db, _catalog);
        var create = Assert.IsType<CreatedResult>(await controller.CreateBrand(
            new UpsertBrandDto("علامة الاختبار", "Test Brand", "test-brand", null, true), default));
        var id = (Guid)create.Value!.GetType().GetProperty("Id")!.GetValue(create.Value)!;

        Assert.IsType<OkObjectResult>(await controller.UpdateBrand(id,
            new UpsertBrandDto("علامة محدثة", "Updated Brand", "test-brand", null, true), default));
        Assert.IsType<NoContentResult>(await controller.ArchiveBrand(id, default));
        Assert.False((await _db.Brands.IgnoreQueryFilters().SingleAsync(b => b.Id == id)).IsActive);
    }
}
