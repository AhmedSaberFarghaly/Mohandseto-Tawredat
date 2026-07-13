namespace Mohandseto.Api.Application.Orders;

public sealed record OrderListDto(Guid Id, string Number, string Status, decimal Total, int ItemCount,
    DateTime RequiredDate, DateTime CreatedAt, bool CanCancel, bool CanTrack);
public sealed record OrderItemDto(Guid Id, Guid ProductId, Guid? VariantId, string Sku, string NameAr,
    string? VariantName, int Quantity, decimal UnitPrice, decimal LineTotal, string? CustomerNote, int? Rating);
public sealed record OrderHistoryDto(string Status, string? Note, DateTime At);
public sealed record ShipmentEventDto(Guid Id, string Status, string DescriptionAr, string? Location,
    double? Latitude, double? Longitude, DateTime At);
public sealed record ShipmentDto(Guid Id, string Number, string CarrierName, string? TrackingNumber, string Status,
    string? DriverName, string? DriverPhone, double? DriverLatitude, double? DriverLongitude,
    DateTime? EstimatedArrival, DateTime? DeliveredAt, IReadOnlyList<ShipmentEventDto> Events);
public sealed record DeliveryProofDto(Guid Id, string Type, string? RecipientName, string? Note, DateTime At, bool HasFile);
public sealed record OrderIssueDto(Guid Id, Guid? OrderItemId, string Type, int? AffectedQuantity,
    string Description, string Status, DateTime CreatedAt, bool HasPhoto);
public sealed record OrderRatingDto(int DeliveryRating, int ServiceRating, string? Comment);
public sealed record RecurringScheduleDto(Guid Id, string Frequency, int Interval, DateTime NextRunAt,
    DateTime? EndsAt, bool IsActive, bool RequireApprovalEachRun);
public sealed record OrderDetailDto(Guid Id, string Number, string Status, decimal Subtotal, decimal Savings,
    decimal CouponDiscount, decimal TaxIncluded, decimal Shipping, decimal Total, string BranchName,
    string DeliveryAddress, string ReceiverName, string ReceiverPhone, DateTime RequiredDate, string? TimeSlot,
    string ShippingMethod, string PaymentMethod, string? PurchaseOrderNumber, string? InternalReference,
    string? CostCenterCode, string? CostCenterName, string? ProjectCode, string? ProjectName,
    string? RequestingDepartment, string? OrderNote, bool AllowSplitDelivery, bool RequiresApproval,
    bool CanCancel, bool CanTrack, IReadOnlyList<OrderItemDto> Items, IReadOnlyList<OrderHistoryDto> History,
    IReadOnlyList<ShipmentDto> Shipments, IReadOnlyList<DeliveryProofDto> Proofs,
    IReadOnlyList<OrderIssueDto> Issues, OrderRatingDto? Rating, IReadOnlyList<RecurringScheduleDto> Schedules);

public sealed record CancelOrderDto(string Reason, string? Details);
public sealed record ReorderResultDto(int AddedItems, int SkippedItems, Guid? CartId);
public sealed record CreateRecurringScheduleDto(string Frequency, int Interval, DateTime NextRunAt,
    DateTime? EndsAt, bool RequireApprovalEachRun);
public sealed record RateOrderDto(int DeliveryRating, int ServiceRating, string? Comment);
public sealed record RateOrderItemDto(int Rating, string? Comment);
public sealed record RequestDeliveryOtpResultDto(DateTime ExpiresAt, string? DevelopmentCode);
public sealed record ConfirmDeliveryOtpDto(string Code, string? RecipientName);
public sealed record FulfillmentUpdateDto(string Status, string? Note, string? ShipmentNumber,
    string? CarrierName, string? TrackingNumber, string? DriverName, string? DriverPhone,
    double? Latitude, double? Longitude, DateTime? EstimatedArrival, string? Location);
