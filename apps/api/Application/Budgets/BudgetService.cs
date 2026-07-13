using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Budgets;

public sealed class BudgetService(AppDbContext db, ITenantProvider tenantProvider)
{
    public async Task<BudgetSummaryDto> SummaryAsync(int? year, int? month, CancellationToken ct = default)
    {
        var selectedYear = year ?? DateTime.UtcNow.Year; if (selectedYear is < 2020 or > 2100 || month is < 1 or > 12) throw ApiException.BadRequest("الفترة غير صالحة");
        var centers = await db.CostCenters.AsNoTracking().Where(c => c.IsActive && c.PeriodStart.Year <= selectedYear && c.PeriodEnd.Year >= selectedYear).OrderBy(c => c.Code).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Where(o => o.CreatedAt.Year == selectedYear && o.Status != OrderStatus.Cancelled && o.CostCenterId != null).ToListAsync(ct);
        if (month is not null) orders = orders.Where(o => o.CreatedAt.Month == month).ToList();
        var centerDtos = centers.Select(Center).ToList(); var total = centers.Sum(c => c.BudgetAmount); var used = centers.Sum(c => c.UsedAmount); var reserved = centers.Sum(c => c.ReservedAmount);
        var elapsedMonths = selectedYear == DateTime.UtcNow.Year ? Math.Max(1, DateTime.UtcNow.Month) : 12; var average = orders.Sum(o => o.Total) / elapsedMonths; var forecast = used + average * Math.Max(0, 12 - elapsedMonths);
        var monthly = Enumerable.Range(1, 12).Select(m => new BudgetPointDto(new DateTime(selectedYear, m, 1).ToString("MMM"), orders.Where(o => o.CreatedAt.Month == m).Sum(o => o.Total))).ToList();
        var categories = orders.GroupBy(o => string.IsNullOrWhiteSpace(o.RequestingDepartment) ? "غير محدد" : o.RequestingDepartment!).OrderByDescending(g => g.Sum(x => x.Total)).Take(8).Select(g => new BudgetPointDto(g.Key, g.Sum(x => x.Total))).ToList();
        var alerts = BuildAlerts(centers, forecast, total); return new(selectedYear, month, total, used, reserved, Math.Max(0, total - used - reserved), total <= 0 ? 0 : decimal.Round((used + reserved) / total * 100, 1), forecast, monthly, categories, centerDtos, alerts);
    }

    public async Task<BudgetCenterDetailDto> CenterAsync(Guid id, CancellationToken ct = default)
    {
        var center = await db.CostCenters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("مركز التكلفة غير موجود");
        var orders = await db.Orders.AsNoTracking().Where(o => o.CostCenterId == center.Id && o.Status != OrderStatus.Cancelled).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        var mapped = orders.Select(o => new BudgetOrderDto(o.Id, o.Number, o.RequestingDepartment ?? "غير محدد", o.ProjectName, o.Total, o.Status.ToString(), o.CreatedAt)).ToList();
        var departments = orders.GroupBy(o => o.RequestingDepartment ?? "غير محدد").OrderByDescending(g => g.Sum(x => x.Total)).Select(g => new BudgetPointDto(g.Key, g.Sum(x => x.Total))).ToList();
        var months = Enumerable.Range(1, 12).Select(m => new BudgetPointDto(new DateTime(DateTime.UtcNow.Year, m, 1).ToString("MMM"), orders.Where(o => o.CreatedAt.Year == DateTime.UtcNow.Year && o.CreatedAt.Month == m).Sum(o => o.Total))).ToList();
        var activeMonths = Math.Max(1, DateTime.UtcNow.Month); var average = orders.Where(o => o.CreatedAt.Year == DateTime.UtcNow.Year).Sum(o => o.Total) / activeMonths;
        return new(Center(center), mapped, departments, months, average, center.UsedAmount + average * Math.Max(0, 12 - activeMonths));
    }

    public async Task<List<BudgetAlertDto>> AlertsAsync(CancellationToken ct = default)
    {
        var centers = await db.CostCenters.AsNoTracking().Where(c => c.IsActive).ToListAsync(ct); return BuildAlerts(centers, centers.Sum(c => c.UsedAmount), centers.Sum(c => c.BudgetAmount));
    }

    public async Task<BudgetAdjustmentDto> RequestAdjustmentAsync(Guid userId, CreateBudgetAdjustmentDto dto, CancellationToken ct = default)
    {
        var center = await db.CostCenters.FirstOrDefaultAsync(c => c.Id == dto.CostCenterId && c.IsActive, ct) ?? throw ApiException.NotFound("مركز التكلفة غير موجود");
        if (dto.RequestedBudget <= center.BudgetAmount || string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("الميزانية المطلوبة يجب أن تزيد عن الحالية مع ذكر السبب");
        if (await db.BudgetAdjustmentRequests.AnyAsync(r => r.CostCenterId == center.Id && (r.Status == BudgetAdjustmentStatus.Submitted || r.Status == BudgetAdjustmentStatus.UnderReview), ct)) throw ApiException.Conflict("يوجد طلب تعديل قيد المراجعة لهذا المركز");
        var request = new BudgetAdjustmentRequest { TenantId = TenantId(), CostCenterId = center.Id, UserId = userId, CurrentBudget = center.BudgetAmount, RequestedBudget = dto.RequestedBudget, Reason = Clean(dto.Reason, 1500)! };
        db.BudgetAdjustmentRequests.Add(request); await db.SaveChangesAsync(ct); return Map(request, center.Code);
    }

    public async Task<BudgetAdjustmentDto> DecideAsync(Guid staffId, Guid id, BudgetAdjustmentDecisionDto dto, CancellationToken ct = default)
    {
        var request = await db.BudgetAdjustmentRequests.IgnoreQueryFilters().Include(r => r.CostCenter).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct) ?? throw ApiException.NotFound("طلب تعديل الميزانية غير موجود");
        if (request.Status is BudgetAdjustmentStatus.Approved or BudgetAdjustmentStatus.Rejected) throw ApiException.Conflict("تم اتخاذ قرار في الطلب");
        if (dto.Approved) { var approved = dto.ApprovedBudget ?? request.RequestedBudget; if (approved <= request.CurrentBudget) throw ApiException.BadRequest("الميزانية المعتمدة غير صالحة"); request.CostCenter.BudgetAmount = approved; request.Status = BudgetAdjustmentStatus.Approved; }
        else request.Status = BudgetAdjustmentStatus.Rejected;
        request.DecisionNote = Clean(dto.Note, 500); request.DecidedAt = DateTime.UtcNow; request.DecidedBy = staffId;
        db.Notifications.Add(new AppNotification { TenantId = request.TenantId, UserId = request.UserId, Type = "budget.adjustment", Title = dto.Approved ? "تم اعتماد تعديل الميزانية" : "تم رفض تعديل الميزانية", Body = $"{request.CostCenter.Code}: {request.RequestedBudget:0.##} ج.م", EntityType = nameof(CostCenter), EntityId = request.CostCenterId });
        await db.SaveChangesAsync(ct); return Map(request, request.CostCenter.Code);
    }

    private static BudgetCenterDto Center(CostCenter c) { var available = c.BudgetAmount - c.UsedAmount - c.ReservedAmount; var util = c.BudgetAmount <= 0 ? 0 : decimal.Round((c.UsedAmount + c.ReservedAmount) / c.BudgetAmount * 100, 1); return new(c.Id, c.Code, c.NameAr, c.BudgetAmount, c.UsedAmount, c.ReservedAmount, Math.Max(0, available), util, util >= 100 ? "Exceeded" : util >= 80 ? "Warning" : "Healthy", c.PeriodStart, c.PeriodEnd); }
    private static List<BudgetAlertDto> BuildAlerts(IEnumerable<CostCenter> centers, decimal forecast, decimal total) { var alerts = new List<BudgetAlertDto>(); foreach (var c in centers) { var util = c.BudgetAmount <= 0 ? 0 : (c.UsedAmount + c.ReservedAmount) / c.BudgetAmount * 100; if (util >= 100) alerts.Add(new("Critical", $"تجاوز ميزانية {c.Code}", $"بلغ الاستخدام {util:0.#}% من ميزانية {c.NameAr}", c.Id, DateTime.UtcNow)); else if (util >= 80) alerts.Add(new("Warning", $"اقتراب نفاد ميزانية {c.Code}", $"تم استخدام {util:0.#}% من الميزانية", c.Id, DateTime.UtcNow)); } if (total > 0 && forecast > total) alerts.Add(new("Warning", "التوقع السنوي يتجاوز الميزانية", $"المتوقع {forecast:0.##} ج.م مقابل {total:0.##} ج.م", null, DateTime.UtcNow)); return alerts; }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("تعذر تحديد الشركة");
    private static BudgetAdjustmentDto Map(BudgetAdjustmentRequest r, string code) => new(r.Id, r.CostCenterId, code, r.CurrentBudget, r.RequestedBudget, r.Reason, r.Status.ToString(), r.CreatedAt, r.DecisionNote);
    private static string? Clean(string? v, int max) { if (string.IsNullOrWhiteSpace(v)) return null; var value = v.Trim(); return value.Length <= max ? value : value[..max]; }
}
