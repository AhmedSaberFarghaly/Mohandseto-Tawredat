using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum ReturnStatus
{
    Draft, Submitted, UnderReview, Approved, Rejected, PickupScheduled, InTransit, Received,
    Inspecting, RefundApproved, RefundCompleted, ReplacementPreparing, ReplacementShipped,
    ReplacementDelivered, Completed, Cancelled
}
public enum ReturnResolution { Refund, Replacement }
public enum RefundMethod { OriginalPayment, BankTransfer, CreditBalance }
public enum ReturnReason { Damaged, WrongItem, MissingParts, NotAsDescribed, ExcessQuantity, QualityIssue, Other }
public enum RefundTransactionStatus { Pending, Processing, Completed, Failed }

public class ReturnRequest : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid UserId { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Draft;
    public ReturnResolution Resolution { get; set; }
    public RefundMethod? RefundMethod { get; set; }
    public decimal RequestedTotal { get; set; }
    public decimal? ApprovedTotal { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public DateTime? PickupAt { get; set; }
    public string? PickupWindow { get; set; }
    public string? PickupDriverName { get; set; }
    public string? PickupDriverPhone { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? RejectionReason { get; set; }
    public string? InspectionNotes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<ReturnItem> Items { get; set; } = [];
    public ICollection<ReturnAttachment> Attachments { get; set; } = [];
    public ICollection<ReturnStatusHistory> History { get; set; } = [];
}

public class ReturnItem : TenantEntity
{
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int Quantity { get; set; }
    public ReturnReason Reason { get; set; }
    public string? Description { get; set; }
    public decimal UnitRefund { get; set; }
    public decimal LineRefund { get; set; }
    public bool IsEligible { get; set; } = true;
    public string? EligibilityNote { get; set; }
    public bool? InspectionPassed { get; set; }
}

public class ReturnAttachment : TenantEntity
{
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public Guid? ReturnItemId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class ReturnStatusHistory : TenantEntity
{
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public ReturnStatus Status { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? Note { get; set; }
}

public class RefundTransaction : TenantEntity
{
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public RefundMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public RefundTransactionStatus Status { get; set; } = RefundTransactionStatus.Pending;
    public string ProviderReference { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
