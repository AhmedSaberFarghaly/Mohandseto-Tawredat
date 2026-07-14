namespace Mohandseto.Api.Application.AdminInventory;

public sealed record WarehouseDto(Guid Id, string Code, string NameAr, string? NameEn, string Governorate, string Address, bool IsActive, int ProductCount, int TotalUnits, decimal StockValue);
public sealed record SaveWarehouseDto(string Code, string NameAr, string? NameEn, string Governorate, string Address, bool IsActive);
public sealed record InventoryStockDto(Guid Id, Guid WarehouseId, string Warehouse, Guid ProductId, string Sku, string Product, int OnHandQty, int ReservedQty, int AvailableQty, int ReorderLevel, string? ShelfLocation, string? Barcode, decimal UnitCost, decimal Value, string State);
public sealed record InventoryMovementDto(Guid Id, string Number, string Type, Guid WarehouseId, string Warehouse, string? DestinationWarehouse, Guid ProductId, string Sku, string Product, int Quantity, int BalanceAfter, string Reason, string? ReferenceType, DateTime CreatedAt);
public sealed record InventoryBatchDto(Guid Id, string Warehouse, string Sku, string Product, string BatchNumber, int Quantity, DateTime? ManufacturedAt, DateTime? ExpiryAt, string State);
public sealed record InventorySerialDto(Guid Id, string Warehouse, string Sku, string Product, string SerialNumber, string Status, string? BatchNumber);
public sealed record StockCountItemDto(Guid ProductId, string Sku, string Product, int SystemQty, int CountedQty, int Difference, string? Reason);
public sealed record StockCountDto(Guid Id, string Number, Guid WarehouseId, string Warehouse, string Status, DateTime CreatedAt, DateTime? CountedAt, DateTime? ReconciledAt, IReadOnlyList<StockCountItemDto> Items);
public sealed record GoodsReceiptItemDto(Guid ProductId, string Sku, string Product, int ReceivedQty, int AcceptedQty, int DamagedQty, decimal UnitCost, string? BatchNumber, DateTime? ExpiryAt);
public sealed record GoodsReceiptDto(Guid Id, string Number, Guid WarehouseId, string Warehouse, Guid? SupplierId, string? Supplier, string SupplierReference, string Status, DateTime ReceivedAt, DateTime? InspectedAt, string? InspectionNotes, IReadOnlyList<GoodsReceiptItemDto> Items);
public sealed record ProductInventoryOptionDto(Guid Id, string Sku, string Name, decimal CostPrice);
public sealed record SupplierInventoryOptionDto(Guid Id, string Code, string Name);
public sealed record InventoryDashboardDto(decimal TotalValue, int TotalUnits, int ReservedUnits, int LowStockCount, int OutOfStockCount, IReadOnlyList<WarehouseDto> Warehouses, IReadOnlyList<InventoryStockDto> Stocks, IReadOnlyList<InventoryMovementDto> Movements, IReadOnlyList<InventoryBatchDto> Batches, IReadOnlyList<InventorySerialDto> Serials, IReadOnlyList<StockCountDto> Counts, IReadOnlyList<GoodsReceiptDto> Receipts, IReadOnlyList<ProductInventoryOptionDto> Products, IReadOnlyList<SupplierInventoryOptionDto> Suppliers);

public sealed record AdjustStockDto(Guid WarehouseId, Guid ProductId, int Quantity, string Type, string Reason, string? BatchNumber, DateTime? ManufacturedAt, DateTime? ExpiryAt, IReadOnlyList<string>? SerialNumbers);
public sealed record TransferStockDto(Guid FromWarehouseId, Guid ToWarehouseId, Guid ProductId, int Quantity, string Reason);
public sealed record ReserveStockDto(Guid WarehouseId, Guid ProductId, int Quantity, string Action, string Reason, string? ReferenceType, Guid? ReferenceId);
public sealed record SaveStockMetadataDto(int ReorderLevel, string? ShelfLocation, string? Barcode);
public sealed record CreateStockCountDto(Guid WarehouseId);
public sealed record ReconcileStockItemDto(Guid ProductId, int CountedQty, string? Reason);
public sealed record ReconcileStockCountDto(IReadOnlyList<ReconcileStockItemDto> Items);
public sealed record CreateGoodsReceiptItemDto(Guid ProductId, int ReceivedQty, decimal UnitCost, string? BatchNumber, DateTime? ExpiryAt);
public sealed record CreateGoodsReceiptDto(Guid WarehouseId, Guid? SupplierId, string SupplierReference, IReadOnlyList<CreateGoodsReceiptItemDto> Items, Guid? PurchaseOrderId = null);
public sealed record InspectGoodsReceiptItemDto(Guid ProductId, int AcceptedQty, int DamagedQty);
public sealed record InspectGoodsReceiptDto(IReadOnlyList<InspectGoodsReceiptItemDto> Items, string? Notes);
