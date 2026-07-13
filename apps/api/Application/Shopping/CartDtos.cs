namespace Mohandseto.Api.Application.Shopping;

public sealed record AddCartItemDto(Guid ProductId, Guid? VariantId, int Quantity, string? CustomizationJson);
public sealed record UpdateCartItemDto(int Quantity);
public sealed record UpdateCartItemNoteDto(string? Note);
public sealed record ApplyCouponDto(string Code);
public sealed record SaveCartDto(string? Name);
public sealed record SavedCartDto(Guid Id, string Name, DateTime SavedAt, int ItemCount, decimal EstimatedTotal);
public sealed record CartItemDto(
    Guid Id, Guid ProductId, string Slug, string Sku, string NameAr, string? VariantName,
    int Quantity, int MinOrderQty, int AvailableQty, decimal UnitPrice, decimal LineTotal,
    decimal Savings, string UnitName, string StockStatus, string? ImageUrl, bool IsSavedForLater,
    Guid? CustomProductRequestId, string? CustomerNote, decimal? PreviousUnitPrice, bool PriceChanged,
    bool HasAvailabilityIssue);
public sealed record CartDto(
    Guid? Id, IReadOnlyList<CartItemDto> Items, IReadOnlyList<CartItemDto> SavedItems,
    int ItemCount, int TotalQuantity, decimal SubtotalBeforeSavings, decimal Savings,
    decimal Subtotal, decimal TaxIncluded, decimal Shipping, decimal Total, bool EligibleForFreeShipping,
    string? CouponCode, decimal CouponDiscount, string? OrderNote, bool HasPriceChanges, bool HasAvailabilityIssues);
