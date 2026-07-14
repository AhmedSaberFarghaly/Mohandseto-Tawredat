using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminInventory;
using Mohandseto.Api.Application.AdminProcurement;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminProcurementTests : IDisposable
{
    private readonly SqliteConnection _connection; private readonly AppDbContext _db; private readonly AdminInventoryService _inventory; private readonly AdminProcurementService _service;
    private readonly Guid _actor = Guid.NewGuid(); private Product _product = null!; private Warehouse _warehouse = null!; private Supplier _supplier = null!;
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    public AdminProcurementTests() { _connection = new("DataSource=:memory:"); _connection.Open(); _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant()); _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult(); _inventory = new(_db); _service = new(_db, _inventory); SeedAsync().GetAwaiter().GetResult(); }
    private async Task SeedAsync() { _product = await _db.Products.FirstAsync(); _product.StockQty = 0; _warehouse = new() { Code = "PROC", NameAr = "مخزن المشتريات", Governorate = "القاهرة", Address = "العبور" }; _supplier = new() { Code = "SUP-P", NameAr = "مورد المشتريات", ContactName = "أحمد" }; _db.AddRange(_warehouse, _supplier, new User { Id = _actor, FullName = "مسؤول المشتريات", Phone = "01091000001", IsPlatformStaff = true }); await _db.SaveChangesAsync(); }

    [Fact]
    public async Task Procurement_cycle_covers_profile_prices_rating_documents_po_receipts_invoice_match_return_and_performance()
    {
        await _service.SaveSupplierAsync(_supplier.Id, new("SUP-P", "مورد المشتريات المتكامل", "Procurement Supplier", "أحمد", "0100", "s@test.com", "القاهرة", "CR-1", "TAX-1", 4, 30, 100000, true, "مورد معتمد"));
        await _service.ReplaceProductsAsync(_supplier.Id, new([new(_product.Id, "V-SKU", 50, 5, 4, DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(3), true)]));
        await _service.RateAsync(_actor, _supplier.Id, new(5, 4, 5, 4, "أداء جيد"));
        await _service.AddDocumentAsync(_supplier.Id, new("عقد التوريد", "Contract", "/suppliers/contract.pdf", DateTime.UtcNow.AddYears(1)));
        var orderId = await _service.CreateOrderAsync(_actor, new(_supplier.Id, _warehouse.Id, DateTime.UtcNow.AddDays(5), 70, 30, "اختبار أمر الشراء", [new(_product.Id, 10, 50)]));
        await _service.SendOrderAsync(orderId);
        await _service.ReceiveAsync(_actor, orderId, new([new(_product.Id, 4, "LOT-A", DateTime.UtcNow.AddMonths(6))], "DN-1"));
        await _service.ReceiveAsync(_actor, orderId, new([new(_product.Id, 6, "LOT-B", DateTime.UtcNow.AddMonths(7))], "DN-2"));
        foreach (var receipt in await _db.GoodsReceipts.Include(x => x.Items).ToListAsync()) await _inventory.InspectReceiptAsync(_actor, receipt.Id, new(receipt.Items.Select(x => new InspectGoodsReceiptItemDto(x.ProductId, x.ReceivedQty, 0)).ToList(), "مطابق"));
        await _service.CreateInvoiceAsync(new(orderId, "INV-S-1", 600, DateTime.UtcNow.Date, DateTime.UtcNow.AddDays(30)));
        await _service.CreateReturnAsync(_actor, new(orderId, _product.Id, 2, "عيب تصنيع"));

        var dashboard = await _service.DashboardAsync(); var supplier = Assert.Single(dashboard.Suppliers); var order = Assert.Single(dashboard.Orders); var invoice = Assert.Single(dashboard.Invoices);
        Assert.Equal("Received", order.Status); Assert.Equal(10, order.Items.Single().ReceivedQty); Assert.Equal("Matched", invoice.Status); Assert.Equal(600, supplier.Payables); Assert.Equal(8, (await _db.Products.SingleAsync(x => x.Id == _product.Id)).StockQty); Assert.Single(supplier.Products); Assert.Single(supplier.Documents); Assert.True(dashboard.Performance.Single().AcceptanceRate >= 99);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
