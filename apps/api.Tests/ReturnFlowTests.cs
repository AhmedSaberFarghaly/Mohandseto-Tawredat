using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Returns;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class ReturnFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-return-tests", Guid.NewGuid().ToString("N"));
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
    public ReturnFlowTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Return_runs_from_eligibility_and_photos_through_pickup_inspection_and_refund()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Returns Co", Status = TenantStatus.Active };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Return Buyer", Phone = "01040000111", PhoneVerified = true };
        var order = new Order { TenantId = tenantId, UserId = userId, Number = $"ORD-RETURN-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(),
            BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Buyer", ReceiverPhone = "0104", RequiredDate = DateTime.UtcNow.AddDays(-2),
            ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.Card, Status = OrderStatus.Delivered, Subtotal = 1000, Total = 1140 };
        var item = new OrderItem { TenantId = tenantId, ProductId = Guid.NewGuid(), Sku = "RET-SKU", NameAr = "منتج للاختبار", Quantity = 10, UnitPrice = 100, LineTotal = 1000 };
        order.Items.Add(item); order.History.Add(new OrderStatusHistory { TenantId = tenantId, Status = OrderStatus.Delivered, ChangedBy = userId, Note = "delivered" });
        _db.AddRange(tenant, user, order); await _db.SaveChangesAsync();
        var service = new ReturnService(_db, _tenant, new TestEnvironment(_root));

        var eligible = await service.EligibleOrdersAsync(userId); Assert.Single(eligible); Assert.Equal(10, eligible[0].Items[0].EligibleQuantity);
        var request = await service.CreateAsync(userId, new CreateReturnDto(order.Id, "Refund", "OriginalPayment", null,
            [new CreateReturnItemDto(item.Id, 2, "Damaged", "تلف في العبوة")]));
        Assert.Equal(200, request.RequestedTotal); Assert.Equal("Draft", request.Status);
        await Assert.ThrowsAsync<ApiException>(() => service.SubmitAsync(userId, request.Id));
        var bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47 };
        var photo = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "damage.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var attachment = await service.UploadAsync(userId, request.Id, request.Items[0].Id, photo); Assert.Equal("image/png", attachment.ContentType);
        request = await service.SubmitAsync(userId, request.Id); Assert.Equal("Submitted", request.Status);
        request = await service.DecisionAsync(Guid.NewGuid(), request.Id, "review", new ReturnDecisionDto("مراجعة الصور")); Assert.Equal("UnderReview", request.Status);
        request = await service.DecisionAsync(Guid.NewGuid(), request.Id, "approve", new ReturnDecisionDto(null)); Assert.Equal("Approved", request.Status);
        request = await service.SchedulePickupAsync(Guid.NewGuid(), request.Id, new ReturnPickupDto(DateTime.UtcNow.AddDays(1), "10:00-12:00", "Driver", "0111")); Assert.Equal("PickupScheduled", request.Status);
        request = await service.TrackPickupAsync(Guid.NewGuid(), request.Id, new ReturnTrackingDto(30.1, 31.2, "Maadi", "في الطريق")); Assert.True(request.CanTrackPickup);
        request = await service.ProgressAsync(Guid.NewGuid(), request.Id, new ReturnProgressDto("received", null, null)); Assert.Equal("Received", request.Status);
        request = await service.ProgressAsync(Guid.NewGuid(), request.Id, new ReturnProgressDto("inspecting", null, null)); Assert.Equal("Inspecting", request.Status);
        request = await service.InspectAsync(Guid.NewGuid(), request.Id, new ReturnInspectionDto(true, 190, "خصم حالة التغليف")); Assert.Equal("RefundApproved", request.Status);
        request = await service.ProgressAsync(Guid.NewGuid(), request.Id, new ReturnProgressDto("refund-complete", "تم التحويل", "RFND-TEST-1")); Assert.Equal("RefundCompleted", request.Status);
        request = await service.ProgressAsync(Guid.NewGuid(), request.Id, new ReturnProgressDto("complete", null, null)); Assert.Equal("Completed", request.Status); Assert.NotNull(request.CompletedAt);
        var transaction = await _db.RefundTransactions.SingleAsync(); Assert.Equal(190, transaction.Amount); Assert.Equal(RefundTransactionStatus.Completed, transaction.Status);
        Assert.Equal(8, (await service.EligibleOrdersAsync(userId)).Single().Items.Single().EligibleQuantity);
        _tenant.TenantId = Guid.NewGuid(); await Assert.ThrowsAsync<ApiException>(() => service.DetailAsync(userId, request.Id));
    }

    [Fact]
    public async Task Rejected_return_releases_quantity_for_a_new_request()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Reject Co", Status = TenantStatus.Active };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Buyer", Phone = "01050000111", PhoneVerified = true };
        var order = new Order { TenantId = tenantId, UserId = userId, Number = $"ORD-RJ-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(), BranchName = "B", DeliveryAddress = "Giza", ReceiverName = "B", ReceiverPhone = "0105", RequiredDate = DateTime.UtcNow, ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.CashOnDelivery, Status = OrderStatus.Delivered, Total = 100 };
        var item = new OrderItem { TenantId = tenantId, ProductId = Guid.NewGuid(), Sku = "RJ", NameAr = "Item", Quantity = 1, UnitPrice = 100, LineTotal = 100 };
        order.Items.Add(item); order.History.Add(new OrderStatusHistory { TenantId = tenantId, Status = OrderStatus.Delivered }); _db.AddRange(tenant, user, order); await _db.SaveChangesAsync();
        var service = new ReturnService(_db, _tenant, new TestEnvironment(_root));
        var request = await service.CreateAsync(userId, new CreateReturnDto(order.Id, "Replacement", null, null, [new CreateReturnItemDto(item.Id, 1, "WrongItem", null)]));
        request = await service.SubmitAsync(userId, request.Id); request = await service.DecisionAsync(Guid.NewGuid(), request.Id, "reject", new ReturnDecisionDto("المنتج مطابق"));
        Assert.Equal("Rejected", request.Status); Assert.Single(await service.EligibleOrdersAsync(userId));
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
