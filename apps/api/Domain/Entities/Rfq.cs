using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum RfqStatus { Draft, Extracting, NeedsReview, Submitted, UnderReview, ClarificationRequested, Quoted, Negotiating, Accepted, Rejected, Converted, Expired, Cancelled }
public enum RfqItemSource { Catalog, FreeText, Excel, Pdf, Image }
public enum RfqAttachmentType { Excel, Pdf, Image, Other }

public class Rfq : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RequiredDate { get; set; }
    public DateTime QuoteDeadline { get; set; }
    public RfqStatus Status { get; set; } = RfqStatus.Draft;
    public string? DeliveryGovernorate { get; set; }
    public Guid? AcceptedQuoteId { get; set; }
    public Guid? ConvertedOrderId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public ICollection<RfqItem> Items { get; set; } = [];
    public ICollection<RfqAttachment> Attachments { get; set; } = [];
    public ICollection<CustomerQuote> Quotes { get; set; } = [];
    public ICollection<QuoteNegotiation> Negotiations { get; set; } = [];
}

public class RfqItem : TenantEntity
{
    public Guid RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public string DescriptionAr { get; set; } = string.Empty;
    public string? SkuHint { get; set; }
    public decimal Quantity { get; set; }
    public string UnitName { get; set; } = "قطعة";
    public string? Specifications { get; set; }
    public string? PreferredBrand { get; set; }
    public bool AllowAlternatives { get; set; } = true;
    public RfqItemSource Source { get; set; }
    public decimal ExtractionConfidence { get; set; } = 1;
    public bool IsReviewed { get; set; }
}

public class RfqAttachment : TenantEntity
{
    public Guid RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;
    public RfqAttachmentType Type { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ExtractionStatus { get; set; } = "Pending";
    public string? ExtractionError { get; set; }
}

public class SupplierQuoteRequest : TenantEntity
{
    public Guid RfqId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "Sent";
}

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int TypicalLeadDays { get; set; } = 3;
    public decimal Rating { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}

public class SupplierQuote : TenantEntity
{
    public Guid RfqId { get; set; }
    public Guid SupplierId { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = "Received";
    public ICollection<SupplierQuoteItem> Items { get; set; } = [];
}

public class SupplierQuoteItem : TenantEntity
{
    public Guid SupplierQuoteId { get; set; }
    public Guid RfqItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? AlternativeDescription { get; set; }
}

public enum CustomerQuoteStatus { Draft, Sent, Accepted, Rejected, RevisionRequested, Expired }
public class CustomerQuote : TenantEntity
{
    public Guid RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public CustomerQuoteStatus Status { get; set; } = CustomerQuoteStatus.Draft;
    public int CurrentVersion { get; set; }
    public Guid? AcceptedVersionId { get; set; }
    public ICollection<CustomerQuoteVersion> Versions { get; set; } = [];
}

public class CustomerQuoteVersion : TenantEntity
{
    public Guid QuoteId { get; set; }
    public CustomerQuote Quote { get; set; } = null!;
    public int VersionNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public string DiscountType { get; set; } = "None";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime ValidUntil { get; set; }
    public int DeliveryDays { get; set; }
    public string? Terms { get; set; }
    public string? ChangeSummary { get; set; }
    public DateTime SentAt { get; set; }
    public ICollection<CustomerQuoteItem> Items { get; set; } = [];
}

public class CustomerQuoteItem : TenantEntity
{
    public Guid VersionId { get; set; }
    public CustomerQuoteVersion Version { get; set; } = null!;
    public Guid RfqItemId { get; set; }
    public Guid? ProductId { get; set; }
    public string DescriptionAr { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int DeliveryDays { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsAlternative { get; set; }
    public string? AlternativeReason { get; set; }
}

public class RfqTemporaryProduct : TenantEntity
{
    public Guid RfqItemId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? Specifications { get; set; }
    public decimal EstimatedCost { get; set; }
    public int DeliveryDays { get; set; }
}

public class QuoteTemplate : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ValidDays { get; set; } = 14;
    public int DeliveryDays { get; set; } = 5;
    public string? Terms { get; set; }
    public string DiscountType { get; set; } = "None";
    public decimal DiscountValue { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum NegotiationMessageType { Message, CounterOffer, RevisionRequest, Clarification }
public class QuoteNegotiation : TenantEntity
{
    public Guid RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;
    public Guid UserId { get; set; }
    public bool IsStaff { get; set; }
    public NegotiationMessageType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? ProposedTotal { get; set; }
}
