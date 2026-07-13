using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum BudgetAdjustmentStatus { Submitted, UnderReview, Approved, Rejected }
public class BudgetAdjustmentRequest : TenantEntity
{
    public Guid CostCenterId { get; set; }
    public CostCenter CostCenter { get; set; } = null!;
    public Guid UserId { get; set; }
    public decimal CurrentBudget { get; set; }
    public decimal RequestedBudget { get; set; }
    public string Reason { get; set; } = string.Empty;
    public BudgetAdjustmentStatus Status { get; set; } = BudgetAdjustmentStatus.Submitted;
    public string? DecisionNote { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
}
