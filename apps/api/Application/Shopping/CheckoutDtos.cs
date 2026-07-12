namespace Mohandseto.Api.Application.Shopping;

public sealed record CheckoutBranchDto(Guid Id, string Name, string Address, string? Phone, bool IsMain);
public sealed record CheckoutPaymentOptionDto(string Code, string NameAr, bool Enabled, string? Reason);
public sealed record CheckoutOptionsDto(
    Guid SessionId, CartDto Cart, IReadOnlyList<CheckoutBranchDto> Branches,
    IReadOnlyList<CheckoutPaymentOptionDto> PaymentOptions, Guid? BranchId,
    string? ReceiverName, string? ReceiverPhone, DateTime? RequiredDate, string? TimeSlot,
    string ShippingMethod, string? PaymentMethod, string? PurchaseOrderNumber, string? InternalReference);
public sealed record UpdateDeliveryDto(Guid BranchId, string ReceiverName, string ReceiverPhone, DateTime RequiredDate, string TimeSlot, string ShippingMethod);
public sealed record UpdatePaymentDto(string PaymentMethod, string? PurchaseOrderNumber, string? InternalReference);
public sealed record CheckoutReviewDto(
    Guid SessionId, IReadOnlyList<CartItemDto> Items, string BranchName, string DeliveryAddress,
    string ReceiverName, string ReceiverPhone, DateTime RequiredDate, string TimeSlot,
    string ShippingMethod, string PaymentMethod, string? PurchaseOrderNumber,
    decimal Subtotal, decimal Savings, decimal TaxIncluded, decimal Shipping, decimal Total,
    bool RequiresApproval);
public sealed record SubmitCheckoutDto(bool AcceptTerms);
public sealed record OrderCreatedDto(Guid Id, string Number, string Status, bool RequiresApproval, decimal Total, DateTime RequiredDate);
