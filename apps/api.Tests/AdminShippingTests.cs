using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.AdminShipping;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminShippingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminShippingService _service;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-shipping-tests", Guid.NewGuid().ToString("N"));
    private readonly Guid _actor = Guid.NewGuid();
    private Guid _driver;
    private Order _order = null!;
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class EnvironmentStub(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Mohandseto.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public AdminShippingTests()
    {
        Directory.CreateDirectory(_root); _connection = new("DataSource=:memory:"); _connection.Open();
        _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); SeedAsync().GetAwaiter().GetResult(); _service = new(_db, new EnvironmentStub(_root));
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Name = "شركة شحن الاختبار", Status = TenantStatus.Active };
        var company = new Company { Tenant = tenant, TenantId = tenant.Id, LegalName = tenant.Name, Phone = "0226000000" };
        var customer = new User { TenantId = tenant.Id, FullName = "مسؤول الاستلام", Phone = "01096000001", IsActive = true };
        var role = new Role { Code = "delivery_driver", NameAr = "مندوب توصيل", NameEn = "Delivery Driver", IsSystem = true };
        _driver = Guid.NewGuid(); var driver = new User { Id = _driver, FullName = "أحمد المندوب", Phone = "01096000002", IsActive = true, IsPlatformStaff = true };
        driver.Roles.Add(new UserRole { User = driver, Role = role, RoleId = role.Id });
        _order = new Order { TenantId = tenant.Id, Number = "ORD-SHIP-001", UserId = customer.Id, BranchId = Guid.NewGuid(), BranchName = "الرئيسي",
            DeliveryAddress = "مدينة نصر، القاهرة", ReceiverName = "محمد أحمد", ReceiverPhone = "01096000003", RequiredDate = DateTime.UtcNow.AddDays(2),
            Status = OrderStatus.Packing, Total = 1200, Subtotal = 1100, Shipping = 100 };
        _order.Items.Add(new OrderItem { TenantId = tenant.Id, ProductId = Guid.NewGuid(), Sku = "SKU-A", NameAr = "صنف أول", Quantity = 6, UnitPrice = 100, LineTotal = 600 });
        _order.Items.Add(new OrderItem { TenantId = tenant.Id, ProductId = Guid.NewGuid(), Sku = "SKU-B", NameAr = "صنف ثان", Quantity = 5, UnitPrice = 100, LineTotal = 500 });
        _db.AddRange(company, customer, driver, new User { Id = _actor, FullName = "مدير التشغيل", Phone = "01096000004", IsPlatformStaff = true }, _order);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Shipping_cycle_covers_create_assign_route_start_proof_and_confirm()
    {
        var dashboard = await _service.DashboardAsync(); var zone = dashboard.Zones.First();
        var created = await _service.CreateAsync(_actor, new(_order.Id, null, zone.Name, 11, null, DateTime.UtcNow.AddHours(2), 30.051, 31.330, []));
        Assert.Equal("Ready", created.Shipment.Status); Assert.Equal(2, created.Items.Count); Assert.True(created.Shipment.Cost > 0);
        await _service.AssignAsync(_actor, created.Shipment.Id, new(_driver, DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow.AddHours(2)));
        var route = await _service.CreateRouteAsync(_actor, new(_driver, DateTime.UtcNow, 30.0444, 31.2357, [created.Shipment.Id]));
        route = await _service.OptimizeRouteAsync(_actor, route.Id); Assert.Equal("Optimized", route.Status); Assert.True(route.EstimatedMinutes > 0);
        await _service.StartAsync(_actor, created.Shipment.Id, 30.045, 31.25);
        await Assert.ThrowsAsync<ApiException>(() => _service.ConfirmAsync(_actor, created.Shipment.Id, "محمد أحمد"));
        var proof = await _service.AddProofAsync(_actor, created.Shipment.Id, new ShippingProofForm { Type = "Photo", RecipientName = "محمد أحمد", Latitude = 30.051, Longitude = 31.330, File = Upload() });
        Assert.True(proof.HasFile); await _service.ConfirmAsync(_actor, created.Shipment.Id, "محمد أحمد");
        var delivered = await _service.DetailAsync(created.Shipment.Id); Assert.Equal("Delivered", delivered.Shipment.Status); Assert.Single(delivered.Proofs);
        Assert.Equal(OrderStatus.Delivered, (await _db.Orders.FindAsync(_order.Id))!.Status);
        Assert.Contains(await _db.AuditLogs.ToListAsync(), x => x.Action == "shipping.delivery.confirm");
        Assert.True(File.Exists(Path.Combine(_root, "App_Data", (await _db.DeliveryProofs.SingleAsync()).StoredPath!.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task Shipping_guards_split_quantities_and_supports_failed_rescheduled_delivery()
    {
        var created = await _service.CreateAsync(_actor, new(_order.Id, null, null, 10, 100, DateTime.UtcNow.AddHours(1), null, null, []));
        var first = created.Items[0]; var second = created.Items[1];
        await Assert.ThrowsAsync<ApiException>(() => _service.SplitAsync(_actor, created.Shipment.Id, new([
            new(null, 5, [new(first.OrderItemId, first.Quantity)]), new(null, 5, [new(second.OrderItemId, second.Quantity - 1)])])));
        var split = await _service.SplitAsync(_actor, created.Shipment.Id, new([
            new(null, 5, [new(first.OrderItemId, first.Quantity / 2), new(second.OrderItemId, second.Quantity / 2)]),
            new(null, 5, [new(first.OrderItemId, first.Quantity - first.Quantity / 2), new(second.OrderItemId, second.Quantity - second.Quantity / 2)])]));
        Assert.Equal(2, split.Count); var shipment = split[0].Shipment;
        await _service.AssignAsync(_actor, shipment.Id, new(_driver, DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow.AddHours(2)));
        await _service.StartAsync(_actor, shipment.Id, null, null);
        await _service.FailAsync(_actor, shipment.Id, new("المستلم غير متاح", "تم الاتصال دون رد", null, null));
        Assert.Equal(OrderStatus.Delayed, (await _db.Orders.FindAsync(_order.Id))!.Status);
        await _service.RescheduleAsync(_actor, shipment.Id, new(DateTime.UtcNow.AddDays(1), "موعد مؤكد مع العميل"));
        var rescheduled = await _service.DetailAsync(shipment.Id); Assert.Equal("Rescheduled", rescheduled.Shipment.Status);
        Assert.Contains(rescheduled.Events, x => x.Status == "Failed"); Assert.Contains(rescheduled.Events, x => x.Status == "Rescheduled");
    }

    private static IFormFile Upload() { var bytes = new byte[] { 1, 2, 3, 4 }; return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "delivery.jpg") { Headers = new HeaderDictionary(), ContentType = "image/jpeg" }; }
    public void Dispose() { _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
