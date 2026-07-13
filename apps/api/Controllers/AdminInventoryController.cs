using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminInventory;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/inventory"), Authorize(Roles = "super_admin,system_admin,inventory_manager,warehouse_keeper,operations_manager")]
public sealed class AdminInventoryController(AdminInventoryService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<InventoryDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);
    [HttpPost("warehouses")] public Task<WarehouseDto> CreateWarehouse(SaveWarehouseDto dto, CancellationToken ct) => service.SaveWarehouseAsync(null, dto, ct);
    [HttpPut("warehouses/{id:guid}")] public Task<WarehouseDto> UpdateWarehouse(Guid id, SaveWarehouseDto dto, CancellationToken ct) => service.SaveWarehouseAsync(id, dto, ct);
    [HttpPost("adjust")] public async Task<IActionResult> Adjust(AdjustStockDto dto, CancellationToken ct) { await service.AdjustAsync(UserId, dto, ct); return NoContent(); }
    [HttpPost("transfer")] public async Task<IActionResult> Transfer(TransferStockDto dto, CancellationToken ct) { await service.TransferAsync(UserId, dto, ct); return NoContent(); }
    [HttpPost("reserve")] public async Task<IActionResult> Reserve(ReserveStockDto dto, CancellationToken ct) { await service.ReserveAsync(UserId, dto, ct); return NoContent(); }
    [HttpPut("stocks/{id:guid}/metadata")] public async Task<IActionResult> StockMetadata(Guid id, SaveStockMetadataDto dto, CancellationToken ct) { await service.SaveMetadataAsync(id, dto, ct); return NoContent(); }
    [HttpPost("counts")] public Task<StockCountDto> CreateCount(CreateStockCountDto dto, CancellationToken ct) => service.CreateCountAsync(UserId, dto, ct);
    [HttpPost("counts/{id:guid}/reconcile")] public async Task<IActionResult> Reconcile(Guid id, ReconcileStockCountDto dto, CancellationToken ct) { await service.ReconcileCountAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("receipts")] public Task<GoodsReceiptDto> CreateReceipt(CreateGoodsReceiptDto dto, CancellationToken ct) => service.CreateReceiptAsync(UserId, dto, ct);
    [HttpPost("receipts/{id:guid}/inspect")] public async Task<IActionResult> Inspect(Guid id, InspectGoodsReceiptDto dto, CancellationToken ct) { await service.InspectReceiptAsync(UserId, id, dto, ct); return NoContent(); }
}
