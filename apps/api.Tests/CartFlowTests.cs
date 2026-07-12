using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class CartFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly CartService _cart;
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }

    public CartFlowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _cart = new CartService(_db, _tenant);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task Cart_calculates_quantity_tiers_and_supports_saved_items()
    {
        _tenant.TenantId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var product = await _db.Products.Include(p => p.PriceTiers).FirstAsync(p => p.StockQty >= 20);
        var quantity = Math.Max(10, product.MinOrderQty);
        var result = await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, quantity, null));
        var item = Assert.Single(result.Items);
        var expected = product.PriceTiers.Where(t => t.MinQty <= quantity).OrderByDescending(t => t.MinQty).First().UnitPrice;
        Assert.Equal(expected, item.UnitPrice);
        Assert.Equal(expected * quantity, result.Subtotal);
        Assert.True(result.TaxIncluded > 0);

        result = await _cart.SetSavedAsync(userId, item.Id, true);
        Assert.Empty(result.Items); Assert.Single(result.SavedItems); Assert.Equal(0, result.Total);
        result = await _cart.SetSavedAsync(userId, item.Id, false);
        Assert.Single(result.Items); Assert.Empty(result.SavedItems);
    }

    [Fact]
    public async Task Cart_is_tenant_isolated_and_rejects_unavailable_quantity()
    {
        var firstTenant = Guid.NewGuid(); _tenant.TenantId = firstTenant; var userId = Guid.NewGuid();
        var product = await _db.Products.FirstAsync(p => p.StockQty > 0);
        await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, product.MinOrderQty, null));

        _tenant.TenantId = Guid.NewGuid();
        Assert.Empty((await _cart.GetAsync(userId)).Items);
        _tenant.TenantId = firstTenant;
        var itemId = (await _cart.GetAsync(userId)).Items[0].Id;
        var error = await Assert.ThrowsAsync<ApiException>(() => _cart.UpdateAsync(userId, itemId, product.StockQty + 1));
        Assert.Equal(409, error.StatusCode);
    }
}
