using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Admin;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminDashboardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    private sealed class PlatformTenantProvider : ITenantProvider { public Guid? TenantId => null; }

    public AdminDashboardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options,
            new PlatformTenantProvider());
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Dashboard_aggregates_sales_quotes_companies_and_recent_orders()
    {
        var tenant = new Tenant { Name = "شركة التحليلات", Status = TenantStatus.Active };
        var company = new Company
        {
            TenantId = tenant.Id, Tenant = tenant, LegalName = "شركة التحليلات",
            Phone = "01070000001",
        };
        var user = new User
        {
            TenantId = tenant.Id, FullName = "مشتري", Phone = "01070000002", IsActive = true,
        };
        var current = MakeOrder(tenant.Id, user.Id, "ORD-DASH-1", 1250, OrderStatus.Confirmed);
        var cancelled = MakeOrder(tenant.Id, user.Id, "ORD-DASH-2", 500, OrderStatus.Cancelled);
        var previous = MakeOrder(tenant.Id, user.Id, "ORD-DASH-OLD", 1000, OrderStatus.Completed);
        var quote = new Rfq
        {
            TenantId = tenant.Id, UserId = user.Id, Number = "RFQ-DASH-1", Title = "توريد",
            RequiredDate = DateTime.UtcNow.AddDays(5), QuoteDeadline = DateTime.UtcNow.AddDays(2),
            Status = RfqStatus.UnderReview,
        };
        _db.AddRange(tenant, company, user, current, cancelled, previous, quote);
        await _db.SaveChangesAsync();
        previous.CreatedAt = DateTime.UtcNow.AddDays(-10);
        await _db.SaveChangesAsync();

        var result = await new AdminDashboardService(_db).GetAsync(7);

        Assert.Equal(1250, result.Summary.TotalSales.Value);
        Assert.Equal(2, result.Summary.NewOrders.Value);
        Assert.Equal(1, result.Summary.PendingQuotes.Value);
        Assert.Equal(1, result.Summary.ActiveCompanies.Value);
        Assert.Equal(7, result.SalesTrend.Count);
        Assert.Equal("شركة التحليلات", Assert.Single(result.TopCompanies).Company);
        Assert.Equal(2, result.RecentOrders.Count);
    }

    private static Order MakeOrder(Guid tenantId, Guid userId, string number, decimal total, OrderStatus status) =>
        new()
        {
            TenantId = tenantId, UserId = userId, Number = number, BranchId = Guid.NewGuid(), BranchName = "الرئيسي",
            DeliveryAddress = "القاهرة", ReceiverName = "مستلم", ReceiverPhone = "01070000003",
            RequiredDate = DateTime.UtcNow.AddDays(2), ShippingMethod = ShippingMethod.Standard,
            PaymentMethod = PaymentMethod.BankTransfer, Status = status, Subtotal = total, Total = total,
        };

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
