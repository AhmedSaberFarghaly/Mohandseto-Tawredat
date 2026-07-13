using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class OrderInternalNote : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid StaffUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class OrderCommunication : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid StaffUserId { get; set; }
    public string Channel { get; set; } = "Phone";
    public string Direction { get; set; } = "Outbound";
    public string Subject { get; set; } = string.Empty;
    public string? Body { get; set; }
}

public enum AdminOrderRefundStatus { Processed, Failed }
public class AdminOrderRefund : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid ProcessedBy { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "CreditBalance";
    public string Reason { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public AdminOrderRefundStatus Status { get; set; } = AdminOrderRefundStatus.Processed;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ShipmentItem : TenantEntity
{
    public Guid ShipmentId { get; set; }
    public Guid OrderItemId { get; set; }
    public int Quantity { get; set; }
}
