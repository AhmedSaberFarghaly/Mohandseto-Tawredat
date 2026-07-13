namespace Mohandseto.Api.Application.Shopping;

public sealed record CheckoutBranchDto(Guid Id, string Name, string Address, string? Phone, bool IsMain,
    double? Latitude, double? Longitude);
public sealed record CheckoutPaymentOptionDto(string Code, string NameAr, bool Enabled, string? Reason);
public sealed record CheckoutCostCenterDto(Guid Id, string Code, string NameAr, decimal BudgetAmount,
    decimal UsedAmount, decimal ReservedAmount, decimal AvailableAmount, decimal? ApprovalThreshold);
public sealed record CheckoutProjectDto(Guid Id, string Code, string NameAr);
public sealed record CheckoutReceiverDto(Guid Id, string Name, string Phone);
public sealed record CheckoutAttachmentDto(Guid Id, string Name, string ContentType, long SizeBytes, string DownloadUrl);
public sealed record BankTransferInstructionsDto(string BankName, string AccountName, string Iban, string Currency);
public sealed record CheckoutOptionsDto(
    Guid SessionId, CartDto Cart, IReadOnlyList<CheckoutBranchDto> Branches,
    IReadOnlyList<CheckoutPaymentOptionDto> PaymentOptions, IReadOnlyList<CheckoutCostCenterDto> CostCenters,
    IReadOnlyList<CheckoutProjectDto> Projects, IReadOnlyList<CheckoutReceiverDto> Receivers,
    Guid? BranchId, Guid? CostCenterId, Guid? ProjectId,
    string? RequestingDepartment, string? OrderNote, bool AllowSplitDelivery,
    string? ReceiverName, string? ReceiverPhone, DateTime? RequiredDate, string? TimeSlot,
    string ShippingMethod, string? PaymentMethod, string? PurchaseOrderNumber, string? InternalReference,
    Guid? PaymentAttemptId, decimal? CreditPortion, decimal? CardPortion, CheckoutAttachmentDto? PurchaseOrderAttachment,
    BankTransferInstructionsDto BankTransferInstructions, CheckoutAttachmentDto? BankTransferReceipt);
public sealed record UpdateDeliveryDto(Guid BranchId, string ReceiverName, string ReceiverPhone,
    DateTime RequiredDate, string TimeSlot, string ShippingMethod, bool AllowSplitDelivery = false);
public sealed record UpdateCheckoutContextDto(Guid CostCenterId, Guid? ProjectId, string RequestingDepartment,
    string? OrderNote, string? InternalReference);
public sealed record UpdatePaymentDto(string PaymentMethod, string? PurchaseOrderNumber, string? InternalReference,
    Guid? PaymentAttemptId = null, decimal? CreditPortion = null, decimal? CardPortion = null);
public sealed record CreateCheckoutAddressDto(string Name, string Governorate, string City, string AddressLine,
    string? Phone, double? Latitude, double? Longitude, bool IsMain = false);
public sealed record CheckoutReviewDto(
    Guid SessionId, IReadOnlyList<CartItemDto> Items, string BranchName, string DeliveryAddress,
    string ReceiverName, string ReceiverPhone, DateTime RequiredDate, string TimeSlot,
    string ShippingMethod, string PaymentMethod, string? PurchaseOrderNumber,
    string? CostCenterCode, string? CostCenterName, string? ProjectName, string? RequestingDepartment,
    string? OrderNote, bool AllowSplitDelivery, CheckoutAttachmentDto? PurchaseOrderAttachment,
    CheckoutAttachmentDto? BankTransferReceipt,
    decimal Subtotal, decimal Savings, string? CouponCode, decimal CouponDiscount,
    decimal TaxIncluded, decimal Shipping, decimal Total,
    decimal? BudgetAvailable, bool BudgetExceeded, bool RequiresApproval);
public sealed record SubmitCheckoutDto(bool AcceptTerms);
public sealed record OrderCreatedDto(Guid Id, string Number, string Status, bool RequiresApproval, decimal Total, DateTime RequiredDate);
public sealed record CreatePaymentAttemptDto(string IdempotencyKey, decimal Amount);
public sealed record ConfirmPaymentAttemptDto(string PaymentToken);
public sealed record PaymentAttemptDto(Guid Id, string ProviderReference, string Status, decimal Amount,
    string Currency, string? FailureCode, string? FailureMessage);
