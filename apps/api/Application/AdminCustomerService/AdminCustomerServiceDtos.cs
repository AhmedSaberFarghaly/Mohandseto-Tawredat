namespace Mohandseto.Api.Application.AdminCustomerService;

public sealed record ServiceKpisDto(int OpenTickets, int SlaBreaches, int PendingReturns, decimal AverageRating, decimal ResolutionHours, decimal ReturnApprovalRate);
public sealed record AdminReturnItemDto(Guid Id, Guid ProductId, string Sku, string Product, int Quantity, string Reason, string? Description, decimal Refund, bool? InspectionPassed, string? Condition, string? Disposition);
public sealed record AdminReturnAttachmentDto(Guid Id, Guid? ItemId, string Name, string ContentType, long Size, DateTime CreatedAt);
public sealed record AdminReturnHistoryDto(string Status, string? Note, DateTime At);
public sealed record AdminReturnDto(Guid Id, string Number, string OrderNumber, string Company, string Customer, string Status, string Resolution, decimal RequestedTotal, decimal? ApprovedTotal, string PickupAddress, DateTime? PickupAt, string? RejectionReason, string? InspectionNotes, DateTime CreatedAt, IReadOnlyList<AdminReturnItemDto> Items, IReadOnlyList<AdminReturnAttachmentDto> Attachments, IReadOnlyList<AdminReturnHistoryDto> History);
public sealed record AdminSupportMessageDto(Guid Id, string Sender, bool IsStaff, string Body, DateTime At);
public sealed record AdminSupportTicketDto(Guid Id, string Number, string Company, string Customer, string Type, string Priority, string Status, string Subject, string Description, Guid? OrderId, Guid? AssignedStaffId, string? AssignedStaff, DateTime CreatedAt, DateTime? FirstResponseDueAt, DateTime? ResolutionDueAt, DateTime? FirstResponseAt, DateTime? ResolvedAt, DateTime? EscalatedAt, string? EscalationReason, int? Rating, string? RatingComment, bool SlaBreached, IReadOnlyList<AdminSupportMessageDto> Messages);
public sealed record SupportSlaDto(Guid Id, string Type, string Priority, int FirstResponseMinutes, int ResolutionMinutes, bool IsActive, decimal Compliance);
public sealed record SupportTemplateDto(Guid Id, string Title, string? Type, string Body, int UsageCount, bool IsActive);
public sealed record SupportStaffDto(Guid Id, string Name, int OpenTickets, int ResolvedTickets, decimal AverageRating, decimal SlaCompliance);
public sealed record IssueTypeReportDto(string Type, int Count, decimal Percent, decimal AverageResolutionHours);
public sealed record WarehouseOptionDto(Guid Id, string Name);
public sealed record CustomerServiceDashboardDto(ServiceKpisDto Kpis, IReadOnlyList<AdminReturnDto> Returns, IReadOnlyList<AdminSupportTicketDto> Tickets, IReadOnlyList<SupportSlaDto> SlaPolicies, IReadOnlyList<SupportTemplateDto> Templates, IReadOnlyList<SupportStaffDto> Staff, IReadOnlyList<IssueTypeReportDto> IssueTypes, IReadOnlyList<WarehouseOptionDto> Warehouses);

public sealed record AssignSupportStaffDto(Guid StaffUserId);
public sealed record ReplySupportTicketDto(string Body, Guid? TemplateId, bool WaitForCustomer);
public sealed record EscalateSupportTicketDto(string Reason);
public sealed record ChangeSupportStatusDto(string Status);
public sealed record SaveSlaPolicyDto(string Type, string Priority, int FirstResponseMinutes, int ResolutionMinutes, bool IsActive);
public sealed record SaveReplyTemplateDto(string Title, string? Type, string Body, bool IsActive);
public sealed record ReturnDispositionDto(string Disposition, Guid? WarehouseId, string Condition, string? Note);
