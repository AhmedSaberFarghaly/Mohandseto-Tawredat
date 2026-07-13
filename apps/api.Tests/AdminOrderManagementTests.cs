using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminOrders;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Application.Orders;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminOrderManagementTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminOrderService _service;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-admin-orders", Guid.NewGuid().ToString("N"));
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();

    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public AdminOrderManagementTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        var tenantProvider = new PlatformTenant();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, tenantProvider);
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        var env = new TestEnvironment(_root);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Company:TaxNumber"] = "TEST-TAX" }).Build();
        var orders = new OrderService(_db, env, new CartService(_db, tenantProvider));
        _service = new AdminOrderService(_db, orders, new FinanceService(_db, tenantProvider, env, config));
        SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Name = "شركة إدارة الطلبات", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenant.Id, Tenant = tenant, LegalName = "شركة إدارة الطلبات", Phone = "01080000001", TaxCardNo = "TAX-1" };
        var customer = new User { TenantId = tenant.Id, FullName = "عميل الطلب", Phone = "01080000002", Email = "orders@test.com" };
        var staff = new User { Id = _staffId, FullName = "مسؤول العمليات", Phone = "01080000003", IsPlatformStaff = true };
        var product = await _db.Products.FirstAsync(x => x.Status == ProductStatus.Active && x.StockQty >= 20);
        var order = new Order
        {
            Id = _orderId, TenantId = tenant.Id, UserId = customer.Id, Number = "ORD-ADMIN-1", BranchId = Guid.NewGuid(),
            BranchName = "الرئيسي", DeliveryAddress = "القاهرة", ReceiverName = "المستلم", ReceiverPhone = "01080000004",
            RequiredDate = DateTime.UtcNow.AddDays(-1), ShippingMethod = ShippingMethod.Standard,
            PaymentMethod = PaymentMethod.BankTransfer, Status = OrderStatus.Confirmed, Subtotal = product.BasePrice * 2,
            Total = product.BasePrice * 2, PurchaseOrderNumber = "PO-ADMIN-1", AllowSplitDelivery = true,
        };
        order.Items.Add(new OrderItem { Id = _itemId, TenantId = tenant.Id, ProductId = product.Id, Sku = product.Sku,
            NameAr = product.NameAr, Quantity = 2, UnitPrice = product.BasePrice, LineTotal = product.BasePrice * 2 });
        order.History.Add(new OrderStatusHistory { TenantId = tenant.Id, Status = OrderStatus.Confirmed, Note = "created" });
        _db.AddRange(tenant, company, customer, staff, order); await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_order_cycle_covers_filters_edits_collaboration_documents_refund_and_archive()
    {
        var page = await _service.ListAsync("PO-ADMIN", "Confirmed", null, null, null, null, true, false, 1, 20);
        Assert.Single(page.Items); Assert.Equal(1, page.Summary.Late);

        var edited = await _service.UpdateQuantitiesAsync(_staffId, _orderId,
            new UpdateAdminOrderQuantitiesDto([new(_itemId, 4)], "زيادة الكمية"));
        Assert.Equal(4, edited.Products.Single().Quantity);

        var replacement = await _db.Products.FirstAsync(x => x.Status == ProductStatus.Active && x.StockQty >= 10 && x.Id != edited.Products.Single().ProductId);
        edited = await _service.SubstituteAsync(_staffId, _orderId, new(_itemId, replacement.Id, "بديل متاح"));
        Assert.Equal(replacement.Id, edited.Products.Single().ProductId);
        edited = await _service.AssignAsync(_staffId, _orderId, _staffId);
        Assert.Equal("مسؤول العمليات", edited.AssignedStaff);

        await _service.AddNoteAsync(_staffId, _orderId, "أولوية في التجهيز");
        await _service.AddCommunicationAsync(_staffId, _orderId, new("Phone", "Outbound", "تأكيد الموعد", "تم التواصل"));
        var invoice = await _service.IssueInvoiceAsync(_orderId); Assert.StartsWith("INV-", invoice.Number);

        edited = await _service.SplitAsync(_staffId, _orderId, new SplitOrderShipmentsDto([
            new("SHP-A", "Mohandseto", [new(_itemId, 2)]), new("SHP-B", "Mohandseto", [new(_itemId, 2)])
        ]));
        Assert.Equal(2, edited.Shipments.Count); Assert.Single(edited.Notes); Assert.Single(edited.Communications);
        Assert.Single(await _service.PickingAsync(_orderId)); Assert.Single(await _service.PackingAsync(_orderId));

        await _service.CancelAsync(_staffId, _orderId, new("طلب العميل", "تأجيل المشروع"));
        var refund = await _service.RefundAsync(_staffId, _orderId, new(100, "CreditBalance", "إلغاء الطلب"));
        Assert.Equal(100, refund.Amount);
        await _service.ArchiveAsync(_staffId, _orderId, false);
        Assert.Single((await _service.ListAsync(null, null, null, null, null, null, false, true)).Items);
        await _service.ArchiveAsync(_staffId, _orderId, true);
        Assert.Null((await _service.DetailAsync(_orderId)).ArchivedAt);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
