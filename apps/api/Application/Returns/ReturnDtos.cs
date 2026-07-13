namespace Mohandseto.Api.Application.Returns;

public sealed record EligibleReturnItemDto(Guid OrderItemId, string Sku, string NameAr, int OrderedQuantity,
    int ReturnedQuantity, int EligibleQuantity, decimal UnitPrice);
public sealed record EligibleReturnOrderDto(Guid OrderId, string Number, DateTime DeliveredAt, DateTime EligibleUntil,
    string DeliveryAddress, IReadOnlyList<EligibleReturnItemDto> Items);
public sealed record CreateReturnItemDto(Guid OrderItemId, int Quantity, string Reason, string? Description);
public sealed record CreateReturnDto(Guid OrderId, string Resolution, string? RefundMethod, string? PickupAddress,
    IReadOnlyList<CreateReturnItemDto> Items);
public sealed record ReturnListDto(Guid Id, string Number, string OrderNumber, string Status, string Resolution,
    decimal RequestedTotal, decimal? ApprovedTotal, int ItemCount, DateTime CreatedAt, DateTime? PickupAt);
public sealed record ReturnItemDto(Guid Id, Guid OrderItemId, string Sku, string NameAr, int Quantity, string Reason,
    string? Description, decimal UnitRefund, decimal LineRefund, bool IsEligible, string? EligibilityNote, bool? InspectionPassed);
public sealed record ReturnAttachmentDto(Guid Id, Guid? ReturnItemId, string Name, string ContentType, long SizeBytes, DateTime CreatedAt);
public sealed record ReturnHistoryDto(string Status, string? Note, DateTime At);
public sealed record ReturnDetailDto(Guid Id, string Number, Guid OrderId, string OrderNumber, string Status,
    string Resolution, string? RefundMethod, decimal RequestedTotal, decimal? ApprovedTotal, string PickupAddress,
    DateTime? PickupAt, string? PickupWindow, string? PickupDriverName, string? PickupDriverPhone,
    double? PickupLatitude, double? PickupLongitude, string? RejectionReason, string? InspectionNotes,
    DateTime? SubmittedAt, DateTime? ReceivedAt, DateTime? CompletedAt, IReadOnlyList<ReturnItemDto> Items,
    IReadOnlyList<ReturnAttachmentDto> Attachments, IReadOnlyList<ReturnHistoryDto> History,
    bool CanEdit, bool CanCancel, bool CanTrackPickup);
public sealed record ReturnDecisionDto(string? Reason);
public sealed record ReturnPickupDto(DateTime PickupAt, string PickupWindow, string? DriverName, string? DriverPhone);
public sealed record ReturnTrackingDto(double Latitude, double Longitude, string? Location, string? Note);
public sealed record ReturnInspectionDto(bool Passed, decimal? ApprovedTotal, string? Notes);
public sealed record ReturnProgressDto(string Action, string? Note, string? ProviderReference);
