namespace Mohandseto.Api.Application.Budgets;

public sealed record BudgetPointDto(string Label, decimal Amount);
public sealed record BudgetCenterDto(Guid Id, string Code, string Name, decimal Budget, decimal Used,
    decimal Reserved, decimal Available, decimal Utilization, string Health, DateTime PeriodStart, DateTime PeriodEnd);
public sealed record BudgetAlertDto(string Severity, string Title, string Body, Guid? CostCenterId, DateTime At);
public sealed record BudgetSummaryDto(int Year, int? Month, decimal TotalBudget, decimal Used, decimal Reserved,
    decimal Available, decimal Utilization, decimal ForecastEnd, IReadOnlyList<BudgetPointDto> MonthlyTrend,
    IReadOnlyList<BudgetPointDto> CategoryBreakdown, IReadOnlyList<BudgetCenterDto> Centers,
    IReadOnlyList<BudgetAlertDto> Alerts);
public sealed record BudgetOrderDto(Guid OrderId, string Number, string Department, string? Project,
    decimal Total, string Status, DateTime CreatedAt);
public sealed record BudgetCenterDetailDto(BudgetCenterDto Center, IReadOnlyList<BudgetOrderDto> Orders,
    IReadOnlyList<BudgetPointDto> DepartmentBreakdown, IReadOnlyList<BudgetPointDto> MonthlyTrend,
    decimal AverageMonthlySpend, decimal ForecastEnd);
public sealed record CreateBudgetAdjustmentDto(Guid CostCenterId, decimal RequestedBudget, string Reason);
public sealed record BudgetAdjustmentDto(Guid Id, Guid CostCenterId, string CenterCode, decimal CurrentBudget,
    decimal RequestedBudget, string Reason, string Status, DateTime CreatedAt, string? DecisionNote);
public sealed record BudgetAdjustmentDecisionDto(bool Approved, decimal? ApprovedBudget, string? Note);
