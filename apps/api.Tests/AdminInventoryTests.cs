using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminInventory;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminInventoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminInventoryService _service;
    private readonly Guid _actor = Guid.NewGuid();
    private Warehouse _main = null!;
    private Warehouse _secondary = null!;
    private Supplier _supplier = null!;
    private Product _product = null!;
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    public AdminInventoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant()); _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult(); _service = new AdminInventoryService(_db); SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        _main = new Warehouse { Code = "MAIN", NameAr = "المخزن الرئيسي", Governorate = "القاهرة", Address = "العبور" };
        _secondary = new Warehouse { Code = "WEST", NameAr = "المخزن الغربي", Governorate = "الجيزة", Address = "أكتوبر" };
        _supplier = new Supplier { Code = "SUP-T", NameAr = "مورد الاختبار" }; _product = await _db.Products.FirstAsync(); _product.StockQty = 0;
        _db.AddRange(_main, _secondary, _supplier, new User { Id = _actor, FullName = "أمين المخزن", Phone = "01090000001", IsPlatformStaff = true }); await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Stock_ledger_handles_add_reserve_release_transfer_deduct_tracking_and_metadata()
    {
        await _service.AdjustAsync(_actor, new(_main.Id, _product.Id, 20, "Add", "رصيد شراء", "B-2026", DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(9), ["SN-100", "SN-101"]));
        await _service.ReserveAsync(_actor, new(_main.Id, _product.Id, 5, "Reserve", "حجز طلب", "order", Guid.NewGuid()));
        await _service.ReserveAsync(_actor, new(_main.Id, _product.Id, 2, "Release", "تحرير جزئي", "order", Guid.NewGuid()));
        await _service.TransferAsync(_actor, new(_main.Id, _secondary.Id, _product.Id, 4, "تغذية مخزن أكتوبر"));
        await _service.AdjustAsync(_actor, new(_secondary.Id, _product.Id, 1, "Deduct", "صرف عينة", null, null, null, null));
        var stock = await _db.WarehouseStocks.SingleAsync(x => x.WarehouseId == _main.Id && x.ProductId == _product.Id);
        await _service.SaveMetadataAsync(stock.Id, new(7, "A-01-03", "622100000001"));

        var dashboard = await _service.DashboardAsync(); var main = dashboard.Stocks.Single(x => x.WarehouseId == _main.Id); var secondary = dashboard.Stocks.Single(x => x.WarehouseId == _secondary.Id);
        Assert.Equal(16, main.OnHandQty); Assert.Equal(3, main.ReservedQty); Assert.Equal(3, secondary.OnHandQty); Assert.Equal("A-01-03", main.ShelfLocation);
        Assert.Equal(6, dashboard.Movements.Count); Assert.Single(dashboard.Batches); Assert.Equal(2, dashboard.Serials.Count); Assert.Equal(19, (await _db.Products.SingleAsync(x => x.Id == _product.Id)).StockQty);
    }

    [Fact]
    public async Task Count_and_supplier_receipt_reconcile_accept_and_reject_quantities_once()
    {
        await _service.AdjustAsync(_actor, new(_main.Id, _product.Id, 10, "Add", "رصيد أولي", null, null, null, null));
        var count = await _service.CreateCountAsync(_actor, new(_main.Id));
        await _service.ReconcileCountAsync(_actor, count.Id, new([new(_product.Id, 8, "عجز جرد")]));
        var receipt = await _service.CreateReceiptAsync(_actor, new(_main.Id, _supplier.Id, "PO-778", [new(_product.Id, 12, 55, "LOT-778", DateTime.UtcNow.AddMonths(6))]));
        await _service.InspectReceiptAsync(_actor, receipt.Id, new([new(_product.Id, 10, 2)], "عبوتان تالفتان"));

        var dashboard = await _service.DashboardAsync();
        Assert.Equal("Reconciled", dashboard.Counts.Single().Status); Assert.Equal(-2, dashboard.Counts.Single().Items.Single().Difference);
        Assert.Equal("PartiallyAccepted", dashboard.Receipts.Single().Status); Assert.Equal(18, dashboard.Stocks.Single().OnHandQty);
        Assert.Contains(dashboard.Movements, x => x.Type == "RejectDamaged" && x.Quantity == 2);
        await Assert.ThrowsAsync<Mohandseto.Api.Application.Common.ApiException>(() => _service.InspectReceiptAsync(_actor, receipt.Id, new([new(_product.Id, 10, 2)], null)));
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
