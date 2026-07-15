using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.AdminAccounting;
using Mohandseto.Api.Application.AdminCustomerService;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AccountingCustomerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-accounting-support-tests", Guid.NewGuid().ToString("N"));
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class EnvironmentStub(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public AccountingCustomerServiceTests()
    {
        Directory.CreateDirectory(_root); _connection = new("DataSource=:memory:"); _connection.Open();
        _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant()); _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Accounting_covers_invoice_transfer_entries_aging_profit_and_period_close()
    {
        var tenant = new Tenant { Name = "Accounting Co", Status = TenantStatus.Active }; var company = new Company { TenantId = tenant.Id, Tenant = tenant, LegalName = tenant.Name, Phone = "01070000001" }; var user = new User { TenantId = tenant.Id, FullName = "Buyer", Phone = "01070000002" };
        var category = new Category { NameAr = "اختبار", NameEn = "Test", Slug = $"acc-cat-{Guid.NewGuid():N}" }; var unit = new UnitOfMeasure { Code = $"ACC-{Guid.NewGuid():N}"[..12], NameAr = "قطعة", NameEn = "Piece" };
        var product = new Product { Sku = "ACC-SKU", Slug = $"acc-{Guid.NewGuid():N}", NameAr = "Accounting Product", Category = category, CategoryId = category.Id, Unit = unit, UnitId = unit.Id, BasePrice = 100, CostPrice = 60, StockQty = 100 };
        var order = new Order { TenantId = tenant.Id, UserId = user.Id, Number = $"ORD-ACC-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(), BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Buyer", ReceiverPhone = user.Phone, RequiredDate = DateTime.UtcNow.AddDays(2), Status = OrderStatus.Confirmed, Subtotal = 1000, TaxIncluded = 140, Total = 1140 };
        order.Items.Add(new OrderItem { TenantId = tenant.Id, ProductId = product.Id, Sku = product.Sku, NameAr = product.NameAr, Quantity = 10, UnitPrice = 100, LineTotal = 1000 }); _db.AddRange(company, user, product, order); await _db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Company:TaxNumber"] = "TAX-ACC" }).Build(); var finance = new FinanceService(_db, new PlatformTenant(), new EnvironmentStub(_root), config); var service = new AdminAccountingService(_db, finance); var actor = Guid.NewGuid();
        var invoice = await service.CreateInvoiceAsync(new(order.Id, DateTime.UtcNow.AddDays(15), "BUYER-TAX")); Assert.Equal(1140, invoice.Total);
        var transfer = await service.RecordTransferAsync(new(invoice.Id, 1140, "BANK-ACC-1")); await finance.DecidePaymentAsync(actor, transfer.Id, new(true, null));
        await service.CreateEntryAsync(actor, new("Expense", null, null, null, "تشغيل", "مصروف اختبار", "EXP-1", 100, 14, DateTime.UtcNow, true));
        var dashboard = await service.DashboardAsync(); Assert.Equal(1140, dashboard.Kpis.Revenue); Assert.Equal(1140, dashboard.Kpis.Collected); Assert.Equal(100, dashboard.Kpis.Expenses); Assert.Single(dashboard.ProductProfits); Assert.Empty(dashboard.Aging);
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1); var period = await service.ClosePeriodAsync(actor, new(start, DateTime.UtcNow.Date, true, "تمت المراجعة")); Assert.Equal("Closed", period.Status); Assert.Equal(1140, period.Revenue); Assert.Equal(100, period.Expenses);
        await Assert.ThrowsAsync<ApiException>(() => service.ClosePeriodAsync(actor, new(start, DateTime.UtcNow.Date, true, null)));
    }

    [Fact]
    public async Task Customer_service_covers_dashboard_assignment_chat_sla_templates_and_restock()
    {
        var tenant = new Tenant { Name = "Support Co", Status = TenantStatus.Active }; var company = new Company { TenantId = tenant.Id, Tenant = tenant, LegalName = tenant.Name, Phone = "01080000001" }; var customer = new User { TenantId = tenant.Id, FullName = "Customer", Phone = "01080000002" }; var staff = new User { FullName = "Support Agent", Phone = "01080000003", IsPlatformStaff = true, IsActive = true };
        var category = new Category { NameAr = "مرتجعات", NameEn = "Returns", Slug = $"sup-cat-{Guid.NewGuid():N}" }; var unit = new UnitOfMeasure { Code = $"SUP-{Guid.NewGuid():N}"[..12], NameAr = "قطعة", NameEn = "Piece" };
        var product = new Product { Sku = "SUP-SKU", Slug = $"sup-{Guid.NewGuid():N}", NameAr = "Returned Product", Category = category, CategoryId = category.Id, Unit = unit, UnitId = unit.Id, BasePrice = 100, CostPrice = 55, StockQty = 20 }; var warehouse = new Warehouse { Code = "RET-WH", NameAr = "مخزن المرتجعات", Governorate = "Cairo", Address = "Cairo" };
        var order = new Order { TenantId = tenant.Id, UserId = customer.Id, Number = $"ORD-SUP-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(), BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = customer.FullName, ReceiverPhone = customer.Phone, RequiredDate = DateTime.UtcNow, Status = OrderStatus.Delivered, Total = 200 };
        var orderItem = new OrderItem { TenantId = tenant.Id, ProductId = product.Id, Sku = product.Sku, NameAr = product.NameAr, Quantity = 2, UnitPrice = 100, LineTotal = 200 }; order.Items.Add(orderItem);
        var returned = new ReturnRequest { TenantId = tenant.Id, Order = order, OrderId = order.Id, UserId = customer.Id, Number = $"RET-{Guid.NewGuid():N}", Status = ReturnStatus.Inspecting, Resolution = ReturnResolution.Refund, RequestedTotal = 100, PickupAddress = "Cairo" }; var returnItem = new ReturnItem { TenantId = tenant.Id, ReturnRequest = returned, ReturnRequestId = returned.Id, OrderItem = orderItem, OrderItemId = orderItem.Id, Quantity = 1, Reason = ReturnReason.Damaged, UnitRefund = 100, LineRefund = 100, InspectionPassed = true }; returned.Items.Add(returnItem);
        var ticket = new SupportTicket { TenantId = tenant.Id, UserId = customer.Id, Number = $"SUP-{Guid.NewGuid():N}", Type = SupportTicketType.Product, Priority = SupportTicketPriority.High, Subject = "مشكلة منتج", Description = "المنتج تالف", FirstResponseDueAt = DateTime.UtcNow.AddMinutes(30), ResolutionDueAt = DateTime.UtcNow.AddHours(8) }; ticket.Messages.Add(new SupportMessage { TenantId = tenant.Id, SenderUserId = customer.Id, Body = ticket.Description });
        _db.AddRange(company, customer, staff, product, warehouse, order, returned, ticket); await _db.SaveChangesAsync(); var service = new AdminCustomerServiceService(_db, new EnvironmentStub(_root));
        await service.AssignAsync(ticket.Id, new(staff.Id)); await service.ReplyAsync(staff.Id, ticket.Id, new("تم استلام بلاغك", null, true)); var sla = await service.SaveSlaAsync(null, new("Product", "High", 30, 480, true)); var template = await service.SaveTemplateAsync(null, new("استلام بلاغ", "Product", "نعمل على حل المشكلة", true));
        await service.SetDispositionAsync(staff.Id, returned.Id, returnItem.Id, new("Restock", warehouse.Id, "سليم وقابل لإعادة البيع", "تم الفحص")); var dashboard = await service.DashboardAsync();
        Assert.Single(dashboard.Returns); Assert.Single(dashboard.Tickets); Assert.Single(dashboard.SlaPolicies); Assert.Single(dashboard.Templates); Assert.Equal(staff.Id, dashboard.Tickets[0].AssignedStaffId); Assert.Equal("WaitingCustomer", dashboard.Tickets[0].Status); Assert.Equal(30, sla.FirstResponseMinutes); Assert.Equal("استلام بلاغ", template.Title);
        Assert.Equal(1, (await _db.WarehouseStocks.SingleAsync()).OnHandQty); Assert.Equal(21, (await _db.Products.SingleAsync()).StockQty); Assert.Equal(ReturnDisposition.Restock, (await _db.ReturnItems.SingleAsync()).Disposition); Assert.Contains(await _db.InventoryMovements.ToListAsync(), x => x.ReferenceType == "customer_return");
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
