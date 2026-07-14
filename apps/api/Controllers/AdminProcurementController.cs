using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminProcurement;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/procurement"), Authorize(Roles = "super_admin,system_admin,procurement_manager,purchasing_officer,operations_manager")]
public sealed class AdminProcurementController(AdminProcurementService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<ProcurementDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);
    [HttpPost("suppliers")] public async Task<IActionResult> CreateSupplier(SaveSupplierDto dto, CancellationToken ct) { await service.SaveSupplierAsync(null, dto, ct); return NoContent(); }
    [HttpPut("suppliers/{id:guid}")] public async Task<IActionResult> UpdateSupplier(Guid id, SaveSupplierDto dto, CancellationToken ct) { await service.SaveSupplierAsync(id, dto, ct); return NoContent(); }
    [HttpPut("suppliers/{id:guid}/products")] public async Task<IActionResult> Products(Guid id, ReplaceSupplierProductsDto dto, CancellationToken ct) { await service.ReplaceProductsAsync(id, dto, ct); return NoContent(); }
    [HttpPost("suppliers/{id:guid}/ratings")] public async Task<IActionResult> Rate(Guid id, RateSupplierDto dto, CancellationToken ct) { await service.RateAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("suppliers/{id:guid}/documents")] public async Task<IActionResult> Document(Guid id, AddSupplierDocumentDto dto, CancellationToken ct) { await service.AddDocumentAsync(id, dto, ct); return NoContent(); }
    [HttpPost("orders")] public async Task<IActionResult> CreateOrder(CreatePurchaseOrderDto dto, CancellationToken ct) { var id = await service.CreateOrderAsync(UserId, dto, ct); return Ok(new { id }); }
    [HttpPost("orders/{id:guid}/send")] public async Task<IActionResult> Send(Guid id, CancellationToken ct) { await service.SendOrderAsync(id, ct); return NoContent(); }
    [HttpPost("orders/{id:guid}/receive")] public async Task<IActionResult> Receive(Guid id, ReceivePurchaseOrderDto dto, CancellationToken ct) { await service.ReceiveAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("invoices")] public async Task<IActionResult> Invoice(CreateSupplierInvoiceDto dto, CancellationToken ct) { await service.CreateInvoiceAsync(dto, ct); return NoContent(); }
    [HttpPost("returns")] public async Task<IActionResult> Return(CreateSupplierReturnDto dto, CancellationToken ct) { await service.CreateReturnAsync(UserId, dto, ct); return NoContent(); }
}
