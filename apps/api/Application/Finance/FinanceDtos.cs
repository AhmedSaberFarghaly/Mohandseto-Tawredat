namespace Mohandseto.Api.Application.Finance;

public sealed record InvoiceExportFile(byte[] Content, string ContentType, string FileName);

public sealed record InvoiceListDto(Guid Id, string Number, string OrderNumber, string Status, string Type,
    decimal Total, decimal PaidAmount, decimal Outstanding, DateTime IssuedAt, DateTime DueAt, bool IsOverdue);
public sealed record InvoiceLineDto(Guid Id, string Sku, string Description, int Quantity, decimal UnitPrice,
    decimal TaxAmount, decimal LineTotal);
public sealed record InvoicePaymentDto(Guid Id, decimal Amount, string Method, string Status, string Reference,
    string? BankReference, DateTime CreatedAt, DateTime? VerifiedAt, bool HasReceipt, string? RejectionReason);
public sealed record InvoiceDetailDto(Guid Id, string Number, Guid OrderId, string OrderNumber, string Status,
    string Type, DateTime IssuedAt, DateTime DueAt, string Currency, string SellerTaxNumber, string? BuyerTaxNumber,
    string? EtaUuid, decimal Subtotal, decimal Discount, decimal Tax, decimal Shipping, decimal Total,
    decimal PaidAmount, decimal Outstanding, IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments, string ElectronicQrPayload);
public sealed record FinanceSummaryDto(decimal Outstanding, decimal Overdue, decimal DueSoon, int OpenInvoices,
    decimal CreditLimit, decimal CreditUsed, decimal CreditAvailable, decimal CreditUtilization,
    IReadOnlyList<InvoiceListDto> Upcoming, IReadOnlyList<InvoicePaymentDto> RecentPayments);
public sealed record StartInvoicePaymentDto(decimal Amount, string? BankReference);
public sealed record InvoicePaymentStartedDto(Guid PaymentId, string Reference, decimal Amount, string Status,
    string BankName, string AccountName, string Iban);
public sealed record CreditLimitRequestDto(decimal RequestedLimit, string Reason);
public sealed record CreditLimitRequestResultDto(Guid Id, decimal CurrentLimit, decimal RequestedLimit,
    string Status, DateTime CreatedAt, string? DecisionNote);
public sealed record PaymentDecisionDto(bool Approved, string? Note);
public sealed record CreditDecisionDto(bool Approved, decimal? ApprovedLimit, string? Note);
