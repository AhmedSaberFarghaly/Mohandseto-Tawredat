using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled }
public enum InvoiceType { Standard, Tax, CreditNote }
public enum InvoicePaymentStatus { Initiated, PendingVerification, Completed, Rejected, Failed }
public enum CreditLimitRequestStatus { Submitted, UnderReview, Approved, Rejected }

public class Invoice : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid UserId { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Tax;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Currency { get; set; } = "EGP";
    public string SellerTaxNumber { get; set; } = string.Empty;
    public string? BuyerTaxNumber { get; set; }
    public string? EtaUuid { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<InvoicePayment> Payments { get; set; } = [];
}

public class InvoiceLine : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid? OrderItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoicePayment : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "BankTransfer";
    public InvoicePaymentStatus Status { get; set; } = InvoicePaymentStatus.Initiated;
    public string Reference { get; set; } = string.Empty;
    public string? BankReference { get; set; }
    public string? ReceiptStoredPath { get; set; }
    public string? ReceiptOriginalName { get; set; }
    public string? ReceiptContentType { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreditLimitRequest : TenantEntity
{
    public Guid UserId { get; set; }
    public decimal CurrentLimit { get; set; }
    public decimal RequestedLimit { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SupportingDocumentPath { get; set; }
    public CreditLimitRequestStatus Status { get; set; } = CreditLimitRequestStatus.Submitted;
    public string? DecisionNote { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
}
