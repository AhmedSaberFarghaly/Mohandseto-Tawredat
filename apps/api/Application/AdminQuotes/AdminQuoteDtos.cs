namespace Mohandseto.Api.Application.AdminQuotes;

public record AdminQuoteSummaryDto(int Total, int NeedsReview, int AwaitingSuppliers, int Drafts, int Negotiating, int Accepted, int Expired);
public record AdminQuoteRowDto(Guid Id, string Number, string Title, string Company, string Customer, string Status,
    int ItemCount, DateTime Deadline, decimal? LatestTotal, int VersionCount, DateTime CreatedAt);
public record AdminQuotePageDto(IReadOnlyList<AdminQuoteRowDto> Items, int Total, int Page, int PageSize, AdminQuoteSummaryDto Summary);
public record AdminQuoteItemDto(Guid Id, Guid? ProductId, string Description, string? Sku, decimal Quantity, string UnitName,
    string? Specifications, string? PreferredBrand, bool AllowAlternatives, string Source, decimal Confidence, bool IsReviewed,
    bool IsTemporary, decimal? EstimatedCost, int? EstimatedDeliveryDays);
public record AdminSupplierDto(Guid Id, string Code, string Name, string? ContactName, string? Phone, string? Email,
    int TypicalLeadDays, decimal Rating, bool IsActive);
public record AdminSupplierRequestDto(Guid Id, Guid SupplierId, string Supplier, DateTime SentAt, DateTime Deadline, string Status);
public record AdminSupplierQuoteItemDto(Guid RfqItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal, string? AlternativeDescription);
public record AdminSupplierQuoteDto(Guid Id, Guid SupplierId, string Supplier, string Number, decimal Total,
    DateTime ValidUntil, string Status, IReadOnlyList<AdminSupplierQuoteItemDto> Items);
public record AdminCustomerQuoteItemDto(Guid Id, Guid RfqItemId, Guid? ProductId, string Description, decimal Quantity,
    string UnitName, decimal CostPrice, decimal UnitPrice, decimal LineTotal, int DeliveryDays, bool IsAlternative, string? AlternativeReason);
public record AdminCustomerQuoteVersionDto(Guid Id, int Version, decimal Subtotal, decimal Tax, decimal Shipping,
    string DiscountType, decimal DiscountValue, decimal DiscountAmount, decimal Total, DateTime ValidUntil,
    int DeliveryDays, string? Terms, string? ChangeSummary, DateTime SentAt, bool IsExpired,
    IReadOnlyList<AdminCustomerQuoteItemDto> Items);
public record AdminNegotiationDto(Guid Id, string Actor, bool IsStaff, string Type, string Message, decimal? ProposedTotal, DateTime At);
public record AdminQuoteTemplateDto(Guid Id, string Name, string? Description, int ValidDays, int DeliveryDays,
    string? Terms, string DiscountType, decimal DiscountValue, bool IsActive);
public record AdminQuoteDetailDto(Guid Id, string Number, string Title, string? Description, string Status,
    string Company, string Customer, string CustomerPhone, string? CustomerEmail, DateTime RequiredDate,
    DateTime QuoteDeadline, string? DeliveryGovernorate, Guid? AssignedStaffId, string? AssignedStaff,
    string? QuoteNumber, string? QuoteStatus, Guid? ConvertedOrderId, IReadOnlyList<AdminQuoteItemDto> Items,
    IReadOnlyList<AdminSupplierRequestDto> SupplierRequests, IReadOnlyList<AdminSupplierQuoteDto> SupplierQuotes,
    IReadOnlyList<AdminCustomerQuoteVersionDto> Versions, IReadOnlyList<AdminNegotiationDto> Negotiations,
    IReadOnlyList<AdminSupplierDto> Suppliers, IReadOnlyList<AdminQuoteTemplateDto> Templates);

public record ReviewAdminRfqItemDto(string Description, decimal Quantity, string UnitName, string? Specifications,
    string? PreferredBrand, bool AllowAlternatives, bool IsReviewed = true);
public record LinkAdminRfqProductDto(Guid ProductId);
public record CreateTemporaryRfqProductDto(string Name, string? Specifications, decimal EstimatedCost, int DeliveryDays);
public record RequestSupplierPriceDto(Guid? SupplierId, string? SupplierName, string? ContactName, string? Phone,
    string? Email, DateTime Deadline);
public record SupplierPriceLineDto(Guid RfqItemId, decimal UnitPrice, string? AlternativeDescription);
public record RecordSupplierPriceDto(Guid SupplierId, string? Number, DateTime ValidUntil, IReadOnlyList<SupplierPriceLineDto> Items);
public record SaveCustomerQuoteLineDto(Guid RfqItemId, Guid? ProductId, string? Description, decimal CostPrice,
    decimal UnitPrice, int DeliveryDays, bool IsAlternative = false, string? AlternativeReason = null);
public record SaveCustomerQuoteDto(IReadOnlyList<SaveCustomerQuoteLineDto> Items, decimal Shipping, string DiscountType,
    decimal DiscountValue, int ValidDays, int DeliveryDays, string? Terms, string? ChangeSummary, bool SendNow = false);
public record StaffNegotiationDto(string Message, decimal? ProposedTotal = null, string Type = "Message");
public record SaveQuoteTemplateDto(string Name, string? Description, int ValidDays, int DeliveryDays, string? Terms,
    string DiscountType, decimal DiscountValue, bool IsActive = true);
public record AssignQuoteDto(Guid StaffUserId);
public record AdminProductOptionDto(Guid Id, string Sku, string Name, decimal Price, int StockQty, int DeliveryDays);
