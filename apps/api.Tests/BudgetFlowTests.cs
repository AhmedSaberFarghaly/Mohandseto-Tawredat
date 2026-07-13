using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Budgets;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class BudgetFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    public BudgetFlowTests() { _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open(); _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant); _db.Database.EnsureCreated(); }

    [Fact]
    public async Task Budget_dashboard_center_forecast_alert_and_adjustment_are_consistent()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Budget Co", Status = TenantStatus.Active };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Budget User", Phone = "01070000111", PhoneVerified = true };
        var center = new CostCenter { TenantId = tenantId, Code = "OPS", NameAr = "التشغيل", BudgetAmount = 10000, UsedAmount = 8500, ReservedAmount = 500,
            PeriodStart = new DateTime(DateTime.UtcNow.Year, 1, 1), PeriodEnd = new DateTime(DateTime.UtcNow.Year, 12, 31) };
        var order = new Order { TenantId = tenantId, UserId = userId, Number = $"ORD-BUD-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(), BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Buyer", ReceiverPhone = "0107", RequiredDate = DateTime.UtcNow.AddDays(2), ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.CreditLine, Status = OrderStatus.Confirmed, CostCenterId = center.Id, CostCenterCode = center.Code, CostCenterName = center.NameAr, RequestingDepartment = "المشتريات", Total = 2500 };
        _db.AddRange(tenant, user, center, order); await _db.SaveChangesAsync();
        var service = new BudgetService(_db, _tenant);
        var summary = await service.SummaryAsync(DateTime.UtcNow.Year, null); Assert.Equal(90, summary.Utilization); Assert.Contains(summary.Alerts, a => a.Severity == "Warning"); Assert.Equal(2500, summary.CategoryBreakdown.Single().Amount);
        var detail = await service.CenterAsync(center.Id); Assert.Single(detail.Orders); Assert.Equal("المشتريات", detail.DepartmentBreakdown.Single().Label); Assert.True(detail.ForecastEnd >= center.UsedAmount);
        var adjustment = await service.RequestAdjustmentAsync(userId, new CreateBudgetAdjustmentDto(center.Id, 20000, "زيادة حجم التشغيل")); Assert.Equal("Submitted", adjustment.Status);
        adjustment = await service.DecideAsync(Guid.NewGuid(), adjustment.Id, new BudgetAdjustmentDecisionDto(true, 18000, "اعتماد جزئي")); Assert.Equal("Approved", adjustment.Status); Assert.Equal(18000, center.BudgetAmount);
        _tenant.TenantId = Guid.NewGuid(); var other = await service.SummaryAsync(DateTime.UtcNow.Year, null); Assert.Empty(other.Centers);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
