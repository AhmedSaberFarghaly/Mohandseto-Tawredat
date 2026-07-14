using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Governorate { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WarehouseStock : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int OnHandQty { get; set; }
    public int ReservedQty { get; set; }
    public int ReorderLevel { get; set; }
    public string? ShelfLocation { get; set; }
    public string? Barcode { get; set; }
    public int AvailableQty => OnHandQty - ReservedQty;
}

public enum InventoryMovementType { Add, Deduct, TransferOut, TransferIn, Reconcile, Reserve, Release, Receive, RejectDamaged }
public class InventoryMovement : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public InventoryMovementType Type { get; set; }
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
}

public class InventoryBatch : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ManufacturedAt { get; set; }
    public DateTime? ExpiryAt { get; set; }
    public int Quantity { get; set; }
}

public enum InventorySerialStatus { Available, Reserved, Issued, Damaged }
public class InventorySerial : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public InventorySerialStatus Status { get; set; } = InventorySerialStatus.Available;
}

public enum StockCountStatus { Draft, InProgress, Reconciled, Cancelled }
public class StockCount : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public DateTime? CountedAt { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public ICollection<StockCountItem> Items { get; set; } = [];
}

public class StockCountItem : BaseEntity
{
    public Guid StockCountId { get; set; }
    public StockCount StockCount { get; set; } = null!;
    public Guid ProductId { get; set; }
    public int SystemQty { get; set; }
    public int CountedQty { get; set; }
    public string? Reason { get; set; }
}

public enum GoodsReceiptStatus { Draft, Inspection, Accepted, PartiallyAccepted, Rejected }
public class GoodsReceipt : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string SupplierReference { get; set; } = string.Empty;
    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Inspection;
    public Guid ReceivedByUserId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? InspectedAt { get; set; }
    public string? InspectionNotes { get; set; }
    public ICollection<GoodsReceiptItem> Items { get; set; } = [];
}

public class GoodsReceiptItem : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public Guid ProductId { get; set; }
    public int ReceivedQty { get; set; }
    public int AcceptedQty { get; set; }
    public int DamagedQty { get; set; }
    public decimal UnitCost { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryAt { get; set; }
}
