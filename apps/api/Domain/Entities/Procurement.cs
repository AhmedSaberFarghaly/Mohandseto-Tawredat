using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class SupplierProduct : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public string? SupplierSku { get; set; }
    public decimal UnitPrice { get; set; }
    public int MinimumQuantity { get; set; } = 1;
    public int LeadDays { get; set; } = 3;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsPreferred { get; set; }
}
public class SupplierRatingRecord : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid RatedByUserId { get; set; }
    public int Quality { get; set; }
    public int Delivery { get; set; }
    public int Price { get; set; }
    public int Communication { get; set; }
    public string? Comment { get; set; }
}
public class SupplierDocument : BaseEntity
{
    public Guid SupplierId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
public enum PurchaseOrderStatus { Draft, Sent, Confirmed, PartiallyReceived, Received, Cancelled }
public class PurchaseOrder : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime ExpectedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}
public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public int OrderedQty { get; set; }
    public int ReceivedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
public enum SupplierInvoiceStatus { Draft, Matched, Variance, Approved, PartiallyPaid, Paid }
public class SupplierInvoice : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;
    public decimal VarianceAmount { get; set; }
}
public enum SupplierReturnStatus { Draft, Sent, Accepted, Refunded }
public class SupplierReturn : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public SupplierReturnStatus Status { get; set; } = SupplierReturnStatus.Draft;
}
