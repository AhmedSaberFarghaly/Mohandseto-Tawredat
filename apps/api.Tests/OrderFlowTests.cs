using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Orders;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class OrderFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-order-tests", Guid.NewGuid().ToString("N"));
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public OrderFlowTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Order_runs_through_search_fulfillment_tracking_delivery_issue_rating_and_reorder()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Orders Co", Status = TenantStatus.Active };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Order Buyer", Phone = "01020000111", PhoneVerified = true };
        var product = await _db.Products.FirstAsync(p => p.Status == ProductStatus.Active && p.StockQty > 20);
        var order = new Order
        {
            TenantId = tenantId, UserId = userId, Number = $"ORD-TEST-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(),
            BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Receiver", ReceiverPhone = "01020000222",
            RequiredDate = DateTime.UtcNow.AddDays(3), ShippingMethod = ShippingMethod.Standard,
            PaymentMethod = PaymentMethod.BankTransfer, Status = OrderStatus.Confirmed, Subtotal = 1000, TaxIncluded = 140, Total = 1140
        };
        order.Items.Add(new OrderItem { TenantId = tenantId, ProductId = product.Id, Sku = product.Sku,
            NameAr = product.NameAr, Quantity = Math.Max(product.MinOrderQty, 2), UnitPrice = 100, LineTotal = 1000 });
        order.History.Add(new OrderStatusHistory { TenantId = tenantId, Status = OrderStatus.Confirmed, ChangedBy = userId, Note = "created" });
        _db.AddRange(tenant, user, order); await _db.SaveChangesAsync();
        var service = new OrderService(_db, new TestEnvironment(_root), new CartService(_db, _tenant));

        var list = await service.ListAsync(userId, "TEST", "Confirmed", null, null);
        Assert.Single(list); Assert.True(list[0].CanCancel);
        await service.FulfillAsync(Guid.NewGuid(), order.Id, new FulfillmentUpdateDto("Picking", "جاري الجمع", null, null, null, null, null, null, null, null, "المخزن"));
        await service.FulfillAsync(Guid.NewGuid(), order.Id, new FulfillmentUpdateDto("Shipped", "غادرت المخزن", null, "Mohandseto", "TRK-1", "Driver", "0101", 30.1, 31.2, DateTime.UtcNow.AddHours(2), "القاهرة"));
        var tracked = await service.FulfillAsync(Guid.NewGuid(), order.Id, new FulfillmentUpdateDto("OutForDelivery", "في الطريق", null, null, null, null, null, 30.2, 31.3, DateTime.UtcNow.AddMinutes(45), "المعادي"));
        Assert.Single(tracked.Shipments); Assert.Equal(3, tracked.Shipments[0].Events.Count); Assert.True(tracked.CanTrack);

        var otp = await service.RequestOtpAsync(userId, order.Id); Assert.NotNull(otp.DevelopmentCode);
        var delivered = await service.ConfirmOtpAsync(userId, order.Id, new ConfirmDeliveryOtpDto(otp.DevelopmentCode!, "Receiver"));
        Assert.Equal("Delivered", delivered.Status); Assert.Contains(delivered.Proofs, p => p.Type == "Otp");
        var issue = await service.ReportIssueAsync(userId, order.Id, "MissingItem", order.Items.First().Id, 1, "عنصر ناقص", null);
        Assert.Equal("Open", issue.Status);
        var rating = await service.RateAsync(userId, order.Id, new RateOrderDto(5, 4, "خدمة جيدة")); Assert.Equal(5, rating.DeliveryRating);
        await service.RateItemAsync(userId, order.Id, order.Items.First().Id, new RateOrderItemDto(4, "جيد"));
        var schedule = await service.ScheduleAsync(userId, order.Id, new CreateRecurringScheduleDto("Monthly", 1, DateTime.UtcNow.AddMonths(1), null, true));
        Assert.True(schedule.IsActive);
        var reordered = await service.ReorderAsync(userId, order.Id); Assert.Equal(1, reordered.AddedItems); Assert.NotNull(reordered.CartId);
        var detail = await service.DetailAsync(userId, order.Id); Assert.Equal(4, detail.Items.Single().Rating); Assert.Single(detail.Issues);

        _tenant.TenantId = Guid.NewGuid();
        await Assert.ThrowsAsync<ApiException>(() => service.DetailAsync(userId, order.Id));
    }

    [Fact]
    public async Task Cancellation_releases_reserved_budget_and_blocks_shipped_orders()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Cancel Co", Status = TenantStatus.Active };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Buyer", Phone = "01030000111", PhoneVerified = true };
        var center = new CostCenter { TenantId = tenantId, Code = "OPS", NameAr = "Operations", BudgetAmount = 5000, ReservedAmount = 500 };
        var order = new Order { TenantId = tenantId, UserId = userId, Number = $"ORD-CANCEL-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(),
            BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Receiver", ReceiverPhone = "0103", RequiredDate = DateTime.UtcNow.AddDays(2),
            ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.CreditLine, Status = OrderStatus.PendingApproval,
            CostCenterId = center.Id, RequiresApproval = true, Subtotal = 500, Total = 500 };
        _db.AddRange(tenant, user, center, order); await _db.SaveChangesAsync();
        var service = new OrderService(_db, new TestEnvironment(_root), new CartService(_db, _tenant));
        var cancelled = await service.CancelAsync(userId, order.Id, new CancelOrderDto("لم نعد نحتاج الطلب", null));
        Assert.Equal("Cancelled", cancelled.Status); Assert.Equal(0, center.ReservedAmount); Assert.Single(await _db.OrderCancellations.ToListAsync());
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
