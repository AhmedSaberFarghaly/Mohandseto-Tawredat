using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum CrmActivityType { Call, Meeting, Note }

public class CrmActivity : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public CrmActivityType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? ContactUserId { get; set; }
}

public enum CrmTaskStatus { Open, InProgress, Completed, Cancelled }
public enum CrmTaskPriority { Low, Normal, High, Urgent }

public class CrmTask : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public DateTime DueAt { get; set; }
    public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;
    public CrmTaskPriority Priority { get; set; } = CrmTaskPriority.Normal;
    public DateTime? CompletedAt { get; set; }
}

public class CustomerStageHistory : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public CustomerStage FromStage { get; set; }
    public CustomerStage ToStage { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
}
