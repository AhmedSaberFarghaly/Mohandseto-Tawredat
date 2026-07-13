namespace Mohandseto.Api.Application.Approvals;

public sealed record ApprovalListItemDto(Guid Id, string Number, string OrderNumber, decimal Total,
    string RequesterName, string CurrentLevel, string Status, bool BudgetConflict, DateTime DueAt, bool IsOverdue);
public sealed record ApprovalStepDto(Guid Id, int Sequence, string Name, string ApproverName, string Status,
    decimal? AuthorityLimit, DateTime? DecidedAt, bool IsCurrentUser);
public sealed record ApprovalActionDto(Guid Id, string ActorName, string Type, int LevelSequence, string? Comment,
    DateTime CreatedAt);
public sealed record ApprovalAttachmentDto(Guid Id, string Name, long SizeBytes, string DownloadUrl);
public sealed record ApprovalDetailDto(Guid Id, string Number, Guid OrderId, string OrderNumber, decimal Total,
    string RequesterName, string Department, string? CostCenter, decimal? BudgetAvailable, bool BudgetConflict,
    string Status, int CurrentLevel, DateTime DueAt, IReadOnlyList<ApprovalStepDto> Steps,
    IReadOnlyList<ApprovalActionDto> Actions, IReadOnlyList<ApprovalAttachmentDto> Attachments,
    bool CanAct, bool ExceedsAuthority);
public sealed record ApprovalDecisionDto(string? Comment);
public sealed record ApprovalDelegateDto(Guid UserId, string? Comment);
public sealed record ApprovalCommentDto(string Comment);
public sealed record ApprovalUserDto(Guid Id, string Name, string Phone);
