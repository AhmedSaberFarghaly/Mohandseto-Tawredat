using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminCatalogOperationsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminCatalogOperationsService _service;
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    public AdminCatalogOperationsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _service = new AdminCatalogOperationsService(_db); SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Id = _tenantId, Name = "شركة تسعير المنتجات", Status = TenantStatus.Active };
        var company = new Company { TenantId = _tenantId, Tenant = tenant, LegalName = "شركة تسعير المنتجات", Phone = "01070000001" };
        var staff = new User { Id = _staffId, FullName = "مدير المنتجات", Phone = "01070000002", IsPlatformStaff = true };
        _db.AddRange(tenant, company, staff); await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Commercial_product_cycle_covers_cost_packaging_links_company_prices_bulk_updates_and_history()
    {
        var products = await _db.Products.Where(x => x.Status == ProductStatus.Active).Take(3).ToListAsync(); var product = products[0];
        var detail = await _service.SaveAsync(product.Id, new(80, "علبة", 12, 4, "CARTON-001", "ضمان عام", "عنوان SEO", "وصف محرك البحث", "توريدات,شركات"));
        Assert.Equal(48, detail.UnitsPerPackage * detail.PackagesPerCarton); Assert.Equal(80, detail.CostPrice); Assert.True(detail.ProfitMargin > 0);

        detail = await _service.ReplaceLinksAsync(product.Id, new([new(products[1].Id, "Alternative", 0), new(products[2].Id, "Related", 1)]));
        Assert.Equal(2, detail.Links.Count); Assert.Contains(detail.Links, x => x.Type == "Alternative");
        detail = await _service.ReplaceCompanyPricesAsync(product.Id, new([new(_tenantId, product.BasePrice - 5, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddMonths(1))]));
        Assert.Single(detail.CompanyPrices); Assert.Equal(_tenantId, detail.CompanyPrices[0].TenantId);

        var changes = await _service.BulkPricesAsync(_staffId, new([new(product.Id, product.BasePrice + 10), new(products[1].Id, products[1].BasePrice + 20)], "تحديث تكلفة المورد"));
        Assert.Equal(2, changes.Count); Assert.All(changes, x => Assert.Equal("Bulk", x.Source));
        var history = await _service.HistoryAsync([product.Id]); Assert.Single(history); Assert.Equal("مدير المنتجات", history[0].Staff);
        await _service.SetStatusAsync(product.Id, "Draft"); Assert.Equal(ProductStatus.Draft, (await _db.Products.SingleAsync(x => x.Id == product.Id)).Status);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
