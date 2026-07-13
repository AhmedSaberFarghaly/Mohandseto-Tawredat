using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class ApprovalPolicy : TenantEntity
{
    public string NameAr { get; set; } = string.Empty;
    public decimal MinimumAmount { get; set; }
    public bool AppliesOnBudgetConflict { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<ApprovalLevel> Levels { get; set; } = [];
}

public class ApprovalLevel : TenantEntity
{
    public Guid PolicyId { get; set; }
    public ApprovalPolicy Policy { get; set; } = null!;
    public int Sequence { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public decimal? AuthorityLimit { get; set; }
    public int SlaHours { get; set; } = 24;
    public ICollection<ApprovalAssignment> Assignments { get; set; } = [];
}

public class ApprovalAssignment : TenantEntity
{
    public Guid LevelId { get; set; }
    public ApprovalLevel Level { get; set; } = null!;
    public Guid UserId { get; set; }
}

public enum ApprovalRequestStatus { Pending, ChangesRequested, Approved, Rejected, Cancelled }
public enum ApprovalStepStatus { Waiting, Current, Approved, Rejected, ChangesRequested, Skipped }
public enum ApprovalActionType { Submitted, Approved, Rejected, ChangesRequested, Delegated, Commented, Resubmitted, Escalated }

public class ApprovalRequest : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid PolicyId { get; set; }
    public ApprovalPolicy Policy { get; set; } = null!;
    public Guid RequestedByUserId { get; set; }
    public ApprovalRequestStatus Status { get; set; } = ApprovalRequestStatus.Pending;
    public int CurrentLevelSequence { get; set; } = 1;
    public bool HasBudgetConflict { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<ApprovalStep> Steps { get; set; } = [];
    public ICollection<ApprovalAction> Actions { get; set; } = [];
    public ICollection<ApprovalAttachment> Attachments { get; set; } = [];
}

public class ApprovalStep : TenantEntity
{
    public Guid RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;
    public Guid LevelId { get; set; }
    public int Sequence { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public Guid ApproverUserId { get; set; }
    public Guid? DelegatedFromUserId { get; set; }
    public decimal? AuthorityLimit { get; set; }
    public ApprovalStepStatus Status { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class ApprovalAction : TenantEntity
{
    public Guid RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public ApprovalActionType Type { get; set; }
    public int LevelSequence { get; set; }
    public string? Comment { get; set; }
    public Guid? DelegateToUserId { get; set; }
}

public class ApprovalAttachment : TenantEntity
{
    public Guid RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;
    public Guid UploadedByUserId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class ApprovalDelegation : TenantEntity
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AppNotification : TenantEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime? ReadAt { get; set; }
}
