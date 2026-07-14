using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminInventory;

public sealed class AdminInventoryService(AppDbContext db)
{
    public async Task<InventoryDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct);
        var stocks = await db.WarehouseStocks.AsNoTracking().ToListAsync(ct);
        var warehouseNames = warehouses.ToDictionary(x => x.Id, x => x.NameAr); var productMap = products.ToDictionary(x => x.Id);
        var stockDtos = stocks.Where(x => warehouseNames.ContainsKey(x.WarehouseId) && productMap.ContainsKey(x.ProductId)).Select(x => Map(x, warehouseNames[x.WarehouseId], productMap[x.ProductId])).OrderBy(x => x.State).ThenBy(x => x.Product).ToList();
        var movements = await db.InventoryMovements.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync(ct);
        var batches = await db.InventoryBatches.AsNoTracking().OrderBy(x => x.ExpiryAt).ToListAsync(ct);
        var batchNames = batches.ToDictionary(x => x.Id, x => x.BatchNumber);
        var serials = await db.InventorySerials.AsNoTracking().OrderBy(x => x.SerialNumber).ToListAsync(ct);
        var counts = await db.StockCounts.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct);
        var receipts = await db.GoodsReceipts.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.ReceivedAt).Take(100).ToListAsync(ct);
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct); var supplierNames = suppliers.ToDictionary(x => x.Id, x => x.NameAr);
        var warehouseDtos = warehouses.Select(x => { var rows = stockDtos.Where(s => s.WarehouseId == x.Id).ToList(); return new WarehouseDto(x.Id, x.Code, x.NameAr, x.NameEn, x.Governorate, x.Address, x.IsActive, rows.Count, rows.Sum(s => s.OnHandQty), rows.Sum(s => s.Value)); }).ToList();
        return new(stockDtos.Sum(x => x.Value), stockDtos.Sum(x => x.OnHandQty), stockDtos.Sum(x => x.ReservedQty), stockDtos.Count(x => x.State == "Low"), stockDtos.Count(x => x.State == "Out"), warehouseDtos, stockDtos,
            movements.Where(x => warehouseNames.ContainsKey(x.WarehouseId) && productMap.ContainsKey(x.ProductId)).Select(x => new InventoryMovementDto(x.Id, x.Number, x.Type.ToString(), x.WarehouseId, warehouseNames[x.WarehouseId], x.DestinationWarehouseId is { } destination && warehouseNames.TryGetValue(destination, out var destinationName) ? destinationName : null, x.ProductId, productMap[x.ProductId].Sku, productMap[x.ProductId].NameAr, x.Quantity, x.BalanceAfter, x.Reason, x.ReferenceType, x.CreatedAt)).ToList(),
            batches.Where(x => warehouseNames.ContainsKey(x.WarehouseId) && productMap.ContainsKey(x.ProductId)).Select(x => new InventoryBatchDto(x.Id, warehouseNames[x.WarehouseId], productMap[x.ProductId].Sku, productMap[x.ProductId].NameAr, x.BatchNumber, x.Quantity, x.ManufacturedAt, x.ExpiryAt, BatchState(x, DateTime.UtcNow))).ToList(),
            serials.Where(x => warehouseNames.ContainsKey(x.WarehouseId) && productMap.ContainsKey(x.ProductId)).Select(x => new InventorySerialDto(x.Id, warehouseNames[x.WarehouseId], productMap[x.ProductId].Sku, productMap[x.ProductId].NameAr, x.SerialNumber, x.Status.ToString(), x.BatchId is { } batchId && batchNames.TryGetValue(batchId, out var batchNumber) ? batchNumber : null)).ToList(),
            counts.Where(x => warehouseNames.ContainsKey(x.WarehouseId)).Select(x => new StockCountDto(x.Id, x.Number, x.WarehouseId, warehouseNames[x.WarehouseId], x.Status.ToString(), x.CreatedAt, x.CountedAt, x.ReconciledAt, x.Items.Where(i => productMap.ContainsKey(i.ProductId)).Select(i => new StockCountItemDto(i.ProductId, productMap[i.ProductId].Sku, productMap[i.ProductId].NameAr, i.SystemQty, i.CountedQty, i.CountedQty - i.SystemQty, i.Reason)).ToList())).ToList(),
            receipts.Where(x => warehouseNames.ContainsKey(x.WarehouseId)).Select(x => new GoodsReceiptDto(x.Id, x.Number, x.WarehouseId, warehouseNames[x.WarehouseId], x.SupplierId, x.SupplierId is { } supplierId && supplierNames.TryGetValue(supplierId, out var supplierName) ? supplierName : null, x.SupplierReference, x.Status.ToString(), x.ReceivedAt, x.InspectedAt, x.InspectionNotes, x.Items.Where(i => productMap.ContainsKey(i.ProductId)).Select(i => new GoodsReceiptItemDto(i.ProductId, productMap[i.ProductId].Sku, productMap[i.ProductId].NameAr, i.ReceivedQty, i.AcceptedQty, i.DamagedQty, i.UnitCost, i.BatchNumber, i.ExpiryAt)).ToList())).ToList(),
            products.Select(x => new ProductInventoryOptionDto(x.Id, x.Sku, x.NameAr, Cost(x))).ToList(), suppliers.Select(x => new SupplierInventoryOptionDto(x.Id, x.Code, x.NameAr)).ToList());
    }

    public async Task<WarehouseDto> SaveWarehouseAsync(Guid? id, SaveWarehouseDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NameAr) || string.IsNullOrWhiteSpace(dto.Address)) throw ApiException.BadRequest("كود واسم وعنوان المخزن مطلوبة");
        var code = dto.Code.Trim().ToUpperInvariant(); if (await db.Warehouses.AnyAsync(x => x.Id != id && x.Code == code, ct)) throw ApiException.Conflict("كود المخزن مستخدم بالفعل");
        var entity = id is null ? new Warehouse() : await db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("المخزن غير موجود");
        entity.Code = code; entity.NameAr = dto.NameAr.Trim(); entity.NameEn = Empty(dto.NameEn); entity.Governorate = dto.Governorate.Trim(); entity.Address = dto.Address.Trim(); entity.IsActive = dto.IsActive;
        if (id is null) db.Warehouses.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.Code, entity.NameAr, entity.NameEn, entity.Governorate, entity.Address, entity.IsActive, 0, 0, 0);
    }

    public async Task AdjustAsync(Guid actorId, AdjustStockDto dto, CancellationToken ct = default)
    {
        if (dto.Quantity <= 0 || string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("الكمية والسبب مطلوبان");
        if (!Enum.TryParse<InventoryMovementType>(dto.Type, true, out var type) || type is not (InventoryMovementType.Add or InventoryMovementType.Deduct)) throw ApiException.BadRequest("نوع حركة المخزون غير صالح");
        await RequireWarehouseProductAsync(dto.WarehouseId, dto.ProductId, ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var stock = await GetStockAsync(dto.WarehouseId, dto.ProductId, ct); if (type == InventoryMovementType.Deduct && stock.AvailableQty < dto.Quantity) throw ApiException.Conflict("الرصيد المتاح لا يكفي للخصم");
        stock.OnHandQty += type == InventoryMovementType.Add ? dto.Quantity : -dto.Quantity;
        var product = await db.Products.SingleAsync(x => x.Id == dto.ProductId, ct); await AddMovementAsync(actorId, stock, type, dto.Quantity, dto.Reason.Trim(), null, null, null, Cost(product), ct);
        if (type == InventoryMovementType.Add) await AddTrackingAsync(stock, dto.Quantity, dto.BatchNumber, dto.ManufacturedAt, dto.ExpiryAt, dto.SerialNumbers, ct);
        await SyncProductAsync(dto.ProductId, ct); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task TransferAsync(Guid actorId, TransferStockDto dto, CancellationToken ct = default)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId || dto.Quantity <= 0 || string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("بيانات التحويل غير صالحة");
        await RequireWarehouseProductAsync(dto.FromWarehouseId, dto.ProductId, ct); await RequireWarehouseProductAsync(dto.ToWarehouseId, dto.ProductId, ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var source = await GetStockAsync(dto.FromWarehouseId, dto.ProductId, ct); var destination = await GetStockAsync(dto.ToWarehouseId, dto.ProductId, ct); if (source.AvailableQty < dto.Quantity) throw ApiException.Conflict("الرصيد المتاح لا يكفي للتحويل");
        source.OnHandQty -= dto.Quantity; destination.OnHandQty += dto.Quantity; var product = await db.Products.SingleAsync(x => x.Id == dto.ProductId, ct);
        await AddMovementAsync(actorId, source, InventoryMovementType.TransferOut, dto.Quantity, dto.Reason.Trim(), dto.ToWarehouseId, "warehouse_transfer", null, Cost(product), ct);
        await AddMovementAsync(actorId, destination, InventoryMovementType.TransferIn, dto.Quantity, dto.Reason.Trim(), dto.FromWarehouseId, "warehouse_transfer", null, Cost(product), ct);
        await SyncProductAsync(dto.ProductId, ct); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task ReserveAsync(Guid actorId, ReserveStockDto dto, CancellationToken ct = default)
    {
        if (dto.Quantity <= 0 || string.IsNullOrWhiteSpace(dto.Reason) || !Enum.TryParse<InventoryMovementType>(dto.Action, true, out var action) || action is not (InventoryMovementType.Reserve or InventoryMovementType.Release)) throw ApiException.BadRequest("بيانات الحجز غير صالحة");
        await RequireWarehouseProductAsync(dto.WarehouseId, dto.ProductId, ct); var stock = await GetStockAsync(dto.WarehouseId, dto.ProductId, ct);
        if (action == InventoryMovementType.Reserve && stock.AvailableQty < dto.Quantity) throw ApiException.Conflict("الرصيد المتاح لا يكفي للحجز");
        if (action == InventoryMovementType.Release && stock.ReservedQty < dto.Quantity) throw ApiException.Conflict("الكمية المحجوزة لا تكفي للتحرير");
        stock.ReservedQty += action == InventoryMovementType.Reserve ? dto.Quantity : -dto.Quantity; var product = await db.Products.SingleAsync(x => x.Id == dto.ProductId, ct);
        await AddMovementAsync(actorId, stock, action, dto.Quantity, dto.Reason.Trim(), null, dto.ReferenceType, dto.ReferenceId, Cost(product), ct); await db.SaveChangesAsync(ct);
    }

    public async Task SaveMetadataAsync(Guid stockId, SaveStockMetadataDto dto, CancellationToken ct = default)
    {
        if (dto.ReorderLevel < 0) throw ApiException.BadRequest("حد إعادة الطلب غير صالح"); var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x => x.Id == stockId, ct) ?? throw ApiException.NotFound("رصيد المخزون غير موجود");
        var barcode = Empty(dto.Barcode); if (barcode is not null && await db.WarehouseStocks.AnyAsync(x => x.Id != stockId && x.Barcode == barcode, ct)) throw ApiException.Conflict("الباركود مستخدم بالفعل");
        stock.ReorderLevel = dto.ReorderLevel; stock.ShelfLocation = Empty(dto.ShelfLocation); stock.Barcode = barcode; await db.SaveChangesAsync(ct);
    }

    public async Task<StockCountDto> CreateCountAsync(Guid actorId, CreateStockCountDto dto, CancellationToken ct = default)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == dto.WarehouseId, ct) ?? throw ApiException.NotFound("المخزن غير موجود"); var stocks = await db.WarehouseStocks.AsNoTracking().Where(x => x.WarehouseId == dto.WarehouseId).ToListAsync(ct);
        var entity = new StockCount { Number = Number("CNT"), WarehouseId = dto.WarehouseId, Status = StockCountStatus.InProgress, CreatedByUserId = actorId, Items = stocks.Select(x => new StockCountItem { ProductId = x.ProductId, SystemQty = x.OnHandQty, CountedQty = x.OnHandQty }).ToList() };
        db.StockCounts.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.Number, entity.WarehouseId, warehouse.NameAr, entity.Status.ToString(), entity.CreatedAt, null, null, []);
    }

    public async Task ReconcileCountAsync(Guid actorId, Guid id, ReconcileStockCountDto dto, CancellationToken ct = default)
    {
        var count = await db.StockCounts.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("جلسة الجرد غير موجودة"); if (count.Status == StockCountStatus.Reconciled) throw ApiException.Conflict("تمت تسوية هذا الجرد بالفعل");
        if (dto.Items.Select(x => x.ProductId).Distinct().Count() != dto.Items.Count || dto.Items.Any(x => x.CountedQty < 0)) throw ApiException.BadRequest("بيانات الجرد غير صالحة");
        var inputs = dto.Items.ToDictionary(x => x.ProductId); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in count.Items)
        {
            if (!inputs.TryGetValue(item.ProductId, out var input)) continue; var stock = await GetStockAsync(count.WarehouseId, item.ProductId, ct); if (input.CountedQty < stock.ReservedQty) throw ApiException.Conflict("الرصيد الفعلي لا يمكن أن يقل عن المحجوز");
            item.CountedQty = input.CountedQty; item.Reason = Empty(input.Reason); var difference = input.CountedQty - stock.OnHandQty; stock.OnHandQty = input.CountedQty;
            if (difference != 0) { var product = await db.Products.SingleAsync(x => x.Id == item.ProductId, ct); await AddMovementAsync(actorId, stock, InventoryMovementType.Reconcile, Math.Abs(difference), item.Reason ?? "تسوية جرد", null, "stock_count", count.Id, Cost(product), ct); await SyncProductAsync(item.ProductId, ct); }
        }
        count.Status = StockCountStatus.Reconciled; count.CountedAt = DateTime.UtcNow; count.ReconciledAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task<GoodsReceiptDto> CreateReceiptAsync(Guid actorId, CreateGoodsReceiptDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierReference) || dto.Items.Count == 0 || dto.Items.Any(x => x.ReceivedQty <= 0 || x.UnitCost < 0) || dto.Items.Select(x => x.ProductId).Distinct().Count() != dto.Items.Count) throw ApiException.BadRequest("بيانات الاستلام غير صالحة");
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == dto.WarehouseId, ct) ?? throw ApiException.NotFound("المخزن غير موجود"); if (dto.SupplierId is not null && !await db.Suppliers.AnyAsync(x => x.Id == dto.SupplierId, ct)) throw ApiException.BadRequest("المورد غير موجود");
        var productIds = dto.Items.Select(x => x.ProductId).ToList(); if (await db.Products.CountAsync(x => productIds.Contains(x.Id), ct) != productIds.Count) throw ApiException.BadRequest("أحد المنتجات غير موجود");
        if (dto.PurchaseOrderId is not null && !await db.PurchaseOrders.AnyAsync(x => x.Id == dto.PurchaseOrderId, ct)) throw ApiException.BadRequest("أمر الشراء غير موجود");
        var entity = new GoodsReceipt { Number = Number("GRN"), WarehouseId = dto.WarehouseId, SupplierId = dto.SupplierId, PurchaseOrderId = dto.PurchaseOrderId, SupplierReference = dto.SupplierReference.Trim(), ReceivedByUserId = actorId, ReceivedAt = DateTime.UtcNow, Items = dto.Items.Select(x => new GoodsReceiptItem { ProductId = x.ProductId, ReceivedQty = x.ReceivedQty, UnitCost = x.UnitCost, BatchNumber = Empty(x.BatchNumber), ExpiryAt = x.ExpiryAt }).ToList() };
        db.GoodsReceipts.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.Number, entity.WarehouseId, warehouse.NameAr, entity.SupplierId, null, entity.SupplierReference, entity.Status.ToString(), entity.ReceivedAt, null, null, []);
    }

    public async Task InspectReceiptAsync(Guid actorId, Guid id, InspectGoodsReceiptDto dto, CancellationToken ct = default)
    {
        var receipt = await db.GoodsReceipts.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("إذن الاستلام غير موجود"); if (receipt.Status != GoodsReceiptStatus.Inspection) throw ApiException.Conflict("تم فحص إذن الاستلام بالفعل");
        var inputs = dto.Items.ToDictionary(x => x.ProductId); if (inputs.Count != receipt.Items.Count) throw ApiException.BadRequest("يجب فحص كل أصناف الاستلام"); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in receipt.Items)
        {
            if (!inputs.TryGetValue(item.ProductId, out var input) || input.AcceptedQty < 0 || input.DamagedQty < 0 || input.AcceptedQty + input.DamagedQty != item.ReceivedQty) throw ApiException.BadRequest("الكميات المقبولة والتالفة يجب أن تساوي المستلمة");
            item.AcceptedQty = input.AcceptedQty; item.DamagedQty = input.DamagedQty; var product = await db.Products.SingleAsync(x => x.Id == item.ProductId, ct);
            if (input.AcceptedQty > 0) { var stock = await GetStockAsync(receipt.WarehouseId, item.ProductId, ct); stock.OnHandQty += input.AcceptedQty; await AddMovementAsync(actorId, stock, InventoryMovementType.Receive, input.AcceptedQty, "استلام مشتريات مورد", null, "goods_receipt", receipt.Id, item.UnitCost, ct); await AddTrackingAsync(stock, input.AcceptedQty, item.BatchNumber, null, item.ExpiryAt, null, ct); await SyncProductAsync(item.ProductId, ct); }
            if (input.DamagedQty > 0) db.InventoryMovements.Add(new InventoryMovement { Number = Number("MOV"), WarehouseId = receipt.WarehouseId, ProductId = item.ProductId, Type = InventoryMovementType.RejectDamaged, Quantity = input.DamagedQty, UnitCost = item.UnitCost, Reason = "رفض كمية تالفة", ReferenceType = "goods_receipt", ReferenceId = receipt.Id, ActorUserId = actorId });
        }
        var accepted = receipt.Items.Sum(x => x.AcceptedQty); var damaged = receipt.Items.Sum(x => x.DamagedQty); receipt.Status = accepted == 0 ? GoodsReceiptStatus.Rejected : damaged == 0 ? GoodsReceiptStatus.Accepted : GoodsReceiptStatus.PartiallyAccepted; receipt.InspectedAt = DateTime.UtcNow; receipt.InspectionNotes = Empty(dto.Notes);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    private async Task RequireWarehouseProductAsync(Guid warehouseId, Guid productId, CancellationToken ct) { if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, ct)) throw ApiException.BadRequest("المخزن غير موجود أو غير نشط"); if (!await db.Products.AnyAsync(x => x.Id == productId, ct)) throw ApiException.BadRequest("المنتج غير موجود"); }
    private async Task<WarehouseStock> GetStockAsync(Guid warehouseId, Guid productId, CancellationToken ct) { var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId, ct); if (stock is not null) return stock; stock = new WarehouseStock { WarehouseId = warehouseId, ProductId = productId, Barcode = $"{warehouseId.ToString()[..4].ToUpperInvariant()}-{productId.ToString()[..8].ToUpperInvariant()}" }; db.WarehouseStocks.Add(stock); return stock; }
    private async Task AddMovementAsync(Guid actor, WarehouseStock stock, InventoryMovementType type, int quantity, string reason, Guid? destination, string? referenceType, Guid? referenceId, decimal unitCost, CancellationToken ct) { db.InventoryMovements.Add(new InventoryMovement { Number = Number("MOV"), WarehouseId = stock.WarehouseId, DestinationWarehouseId = destination, ProductId = stock.ProductId, Type = type, Quantity = quantity, BalanceAfter = stock.OnHandQty, UnitCost = unitCost, Reason = reason, ReferenceType = referenceType, ReferenceId = referenceId, ActorUserId = actor }); await Task.CompletedTask; }
    private async Task AddTrackingAsync(WarehouseStock stock, int quantity, string? batchNumber, DateTime? manufacturedAt, DateTime? expiryAt, IReadOnlyList<string>? serialNumbers, CancellationToken ct) { InventoryBatch? batch = null; if (!string.IsNullOrWhiteSpace(batchNumber)) { var number = batchNumber.Trim(); batch = await db.InventoryBatches.FirstOrDefaultAsync(x => x.WarehouseId == stock.WarehouseId && x.ProductId == stock.ProductId && x.BatchNumber == number, ct); if (batch is null) { batch = new InventoryBatch { WarehouseId = stock.WarehouseId, ProductId = stock.ProductId, BatchNumber = number, ManufacturedAt = manufacturedAt, ExpiryAt = expiryAt }; db.InventoryBatches.Add(batch); } batch.Quantity += quantity; } var serials = serialNumbers?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? []; if (serials.Count > quantity) throw ApiException.BadRequest("عدد الأرقام التسلسلية لا يمكن أن يتجاوز الكمية"); if (serials.Count > 0 && await db.InventorySerials.AnyAsync(x => serials.Contains(x.SerialNumber), ct)) throw ApiException.Conflict("أحد الأرقام التسلسلية مسجل بالفعل"); db.InventorySerials.AddRange(serials.Select(x => new InventorySerial { WarehouseId = stock.WarehouseId, ProductId = stock.ProductId, BatchId = batch?.Id, SerialNumber = x })); }
    private async Task SyncProductAsync(Guid productId, CancellationToken ct) { var product = await db.Products.SingleAsync(x => x.Id == productId, ct); var persisted = await db.WarehouseStocks.Where(x => x.ProductId == productId).ToListAsync(ct); var pending = db.ChangeTracker.Entries<WarehouseStock>().Where(x => x.State == EntityState.Added && x.Entity.ProductId == productId && persisted.All(y => y.Id != x.Entity.Id)).Select(x => x.Entity); product.StockQty = persisted.Concat(pending).Sum(x => x.OnHandQty); }
    private static InventoryStockDto Map(WarehouseStock x, string warehouse, Product product) { var cost = Cost(product); var available = x.OnHandQty - x.ReservedQty; var state = x.OnHandQty == 0 ? "Out" : available <= x.ReorderLevel ? "Low" : "Healthy"; return new(x.Id, x.WarehouseId, warehouse, x.ProductId, product.Sku, product.NameAr, x.OnHandQty, x.ReservedQty, available, x.ReorderLevel, x.ShelfLocation, x.Barcode, cost, cost * x.OnHandQty, state); }
    private static decimal Cost(Product product) => product.CostPrice > 0 ? product.CostPrice : product.BasePrice;
    private static string BatchState(InventoryBatch x, DateTime now) => x.ExpiryAt is null ? "NoExpiry" : x.ExpiryAt <= now ? "Expired" : x.ExpiryAt <= now.AddDays(30) ? "Expiring" : "Valid";
    private static string Number(string prefix) => $"{prefix}-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
