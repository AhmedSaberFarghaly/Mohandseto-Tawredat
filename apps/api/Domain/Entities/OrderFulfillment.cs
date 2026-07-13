using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class Shipment : TenantEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = "Created";
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public double? DriverLatitude { get; set; }
    public double? DriverLongitude { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public ICollection<ShipmentEvent> Events { get; set; } = [];
}

public class ShipmentEvent : TenantEntity
{
    public Guid ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public enum DeliveryProofType { Photo, Signature, Otp, Document }
public class DeliveryProof : TenantEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DeliveryProofType Type { get; set; }
    public string? RecipientName { get; set; }
    public string? StoredPath { get; set; }
    public string? OriginalName { get; set; }
    public string? ContentType { get; set; }
    public string? Note { get; set; }
}

public class DeliveryConfirmation : TenantEntity
{
    public Guid OrderId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class OrderRating : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public int DeliveryRating { get; set; }
    public int ServiceRating { get; set; }
    public string? Comment { get; set; }
}

public class OrderItemRating : TenantEntity
{
    public Guid OrderItemId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public enum OrderIssueType { MissingItem, WrongItem, DamagedItem, QuantityMismatch, Other }
public enum OrderIssueStatus { Open, UnderReview, Resolved, Rejected }
public class OrderIssue : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid UserId { get; set; }
    public OrderIssueType Type { get; set; }
    public int? AffectedQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public OrderIssueStatus Status { get; set; } = OrderIssueStatus.Open;
    public string? StoredPhotoPath { get; set; }
}

public class OrderCancellation : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CancelledAt { get; set; }
}

public class RecurringOrderSchedule : TenantEntity
{
    public Guid SourceOrderId { get; set; }
    public Guid UserId { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public int Interval { get; set; } = 1;
    public DateTime NextRunAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequireApprovalEachRun { get; set; } = true;
}
