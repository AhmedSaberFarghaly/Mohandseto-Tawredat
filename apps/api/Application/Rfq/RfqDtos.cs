namespace Mohandseto.Api.Application.Rfq;

public sealed record CreateRfqDto(string Title, string? Description, DateTime RequiredDate, DateTime QuoteDeadline, string? DeliveryGovernorate);
public sealed record UpsertRfqItemDto(Guid? ProductId, string Description, decimal Quantity, string UnitName,
    string? Specifications, string? PreferredBrand, bool AllowAlternatives, string Source = "FreeText", bool IsReviewed = true);
public sealed record RfqItemDto(Guid Id, Guid? ProductId, string Description, string? SkuHint, decimal Quantity,
    string UnitName, string? Specifications, string? PreferredBrand, bool AllowAlternatives, string Source,
    decimal Confidence, bool IsReviewed);
public sealed record RfqAttachmentDto(Guid Id, string Name, string Type, long SizeBytes, string ExtractionStatus, string? Error, string DownloadUrl);
public sealed record QuoteItemDto(Guid Id, Guid RfqItemId, Guid? ProductId, string Description, decimal Quantity,
    string UnitName, decimal UnitPrice, decimal LineTotal, bool IsAlternative, string? AlternativeReason);
public sealed record QuoteVersionDto(Guid Id, int Version, decimal Subtotal, decimal Tax, decimal Shipping, decimal Total,
    DateTime ValidUntil, int DeliveryDays, string? Terms, string? ChangeSummary, DateTime SentAt, IReadOnlyList<QuoteItemDto> Items, bool IsExpired);
public sealed record NegotiationDto(Guid Id, Guid UserId, bool IsStaff, string Type, string Message, decimal? ProposedTotal, DateTime CreatedAt);
public sealed record RfqDetailDto(Guid Id, string Number, string Title, string? Description, string Status,
    DateTime RequiredDate, DateTime QuoteDeadline, string? DeliveryGovernorate, IReadOnlyList<RfqItemDto> Items,
    IReadOnlyList<RfqAttachmentDto> Attachments, string? QuoteNumber, string? QuoteStatus,
    IReadOnlyList<QuoteVersionDto> QuoteVersions, IReadOnlyList<NegotiationDto> Negotiations, Guid? ConvertedOrderId);
public sealed record RfqListDto(Guid Id, string Number, string Title, string Status, int ItemCount, DateTime RequiredDate,
    DateTime QuoteDeadline, decimal? LatestQuoteTotal, DateTime CreatedAt);
public sealed record PublishQuoteItemDto(Guid RfqItemId, Guid? ProductId, decimal UnitPrice, bool IsAlternative = false, string? AlternativeReason = null);
public sealed record PublishQuoteDto(IReadOnlyList<PublishQuoteItemDto> Items, decimal Shipping, int ValidDays,
    int DeliveryDays, string? Terms, string? ChangeSummary);
public sealed record NegotiationRequestDto(string Message, decimal? ProposedTotal = null, string Type = "Message");
public sealed record QuoteDecisionDto(Guid VersionId, string? Comment);
public sealed record ConvertRfqDto(Guid BranchId, Guid CostCenterId, string ReceiverName, string ReceiverPhone);
public sealed record ConvertedRfqOrderDto(Guid OrderId, string OrderNumber, string Status, bool RequiresApproval, decimal Total);
public sealed record RfqBranchDto(Guid Id, string Name, string Address);
public sealed record RfqCostCenterDto(Guid Id, string Code, string Name, decimal Available);
public sealed record RfqReceiverDto(Guid Id, string Name, string Phone);
public sealed record RfqConversionOptionsDto(IReadOnlyList<RfqBranchDto> Branches,
    IReadOnlyList<RfqCostCenterDto> CostCenters, IReadOnlyList<RfqReceiverDto> Receivers);
