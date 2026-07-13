using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class CostCenter : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal? ApprovalThreshold { get; set; }
    public DateTime PeriodStart { get; set; } = new(DateTime.UtcNow.Year, 1, 1);
    public DateTime PeriodEnd { get; set; } = new(DateTime.UtcNow.Year, 12, 31);
    public bool IsActive { get; set; } = true;
}

public class CompanyProject : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public enum CheckoutAttachmentType { PurchaseOrder, BankTransferReceipt, Other }

public class CheckoutAttachment : TenantEntity
{
    public Guid CheckoutSessionId { get; set; }
    public CheckoutSession CheckoutSession { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public CheckoutAttachmentType Type { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public enum PaymentAttemptStatus { Created, RequiresAction, Succeeded, Failed, Cancelled }

public class PaymentAttempt : TenantEntity
{
    public Guid CheckoutSessionId { get; set; }
    public CheckoutSession CheckoutSession { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentAttemptStatus Status { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
