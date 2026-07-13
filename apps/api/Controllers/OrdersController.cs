using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Orders;
using Mohandseto.Api.Application.AdminOrders;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/orders")]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<List<OrderListDto>> List([FromQuery] string? search, [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => orders.ListAsync(UserId, search, status, from, to, ct);
    [HttpGet("{id:guid}")] public Task<OrderDetailDto> Detail(Guid id, CancellationToken ct) => orders.DetailAsync(UserId, id, ct);
    [HttpPost("{id:guid}/cancel")] public Task<OrderDetailDto> Cancel(Guid id, CancelOrderDto dto, CancellationToken ct) => orders.CancelAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/reorder")] public Task<ReorderResultDto> Reorder(Guid id, CancellationToken ct) => orders.ReorderAsync(UserId, id, ct);
    [HttpPost("{id:guid}/recurring")] public Task<RecurringScheduleDto> Schedule(Guid id, CreateRecurringScheduleDto dto, CancellationToken ct) => orders.ScheduleAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/issues"), RequestSizeLimit(10 * 1024 * 1024 + 4096)] public Task<OrderIssueDto> Issue(Guid id, [FromForm] string type, [FromForm] Guid? orderItemId, [FromForm] int? affectedQuantity, [FromForm] string description, IFormFile? photo, CancellationToken ct) => orders.ReportIssueAsync(UserId, id, type, orderItemId, affectedQuantity, description, photo, ct);
    [HttpPut("{id:guid}/rating")] public Task<OrderRatingDto> Rate(Guid id, RateOrderDto dto, CancellationToken ct) => orders.RateAsync(UserId, id, dto, ct);
    [HttpPut("{id:guid}/items/{itemId:guid}/rating")] public async Task<IActionResult> RateItem(Guid id, Guid itemId, RateOrderItemDto dto, CancellationToken ct) { await orders.RateItemAsync(UserId, id, itemId, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/delivery-code")] public Task<RequestDeliveryOtpResultDto> DeliveryCode(Guid id, CancellationToken ct) => orders.RequestOtpAsync(UserId, id, ct);
    [HttpPost("{id:guid}/confirm-delivery")] public Task<OrderDetailDto> ConfirmDelivery(Guid id, ConfirmDeliveryOtpDto dto, CancellationToken ct) => orders.ConfirmOtpAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/proofs"), RequestSizeLimit(10 * 1024 * 1024 + 4096)] public Task<DeliveryProofDto> Proof(Guid id, [FromForm] string type, [FromForm] string? recipientName, [FromForm] string? note, IFormFile file, CancellationToken ct) => orders.UploadProofAsync(UserId, id, type, recipientName, note, file, ct);
    [HttpGet("proofs/{proofId:guid}")] public async Task<IActionResult> ProofFile(Guid proofId, CancellationToken ct) { var file = await orders.ProofFileAsync(UserId, proofId, ct); return PhysicalFile(file.Path, file.Type, file.Name, enableRangeProcessing: true); }
}

[ApiController, Authorize(Roles = "super_admin,operations_manager,sales_manager,sales_agent,warehouse_manager,support_agent,auditor"), Route("api/admin/orders")]
public sealed class AdminOrdersController(OrderService orders, AdminOrderService admin) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<AdminOrderPageDto> List([FromQuery] string? search, [FromQuery] string? statuses,
        [FromQuery] Guid? tenantId, [FromQuery] Guid? assignedStaffId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] bool overdue = false, [FromQuery] bool archived = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        admin.ListAsync(search, statuses, tenantId, assignedStaffId, from, to, overdue, archived, page, pageSize, ct);
    [HttpGet("{id:guid}")] public Task<AdminOrderDetailDto> Detail(Guid id, CancellationToken ct) => admin.DetailAsync(id, ct);
    [HttpPost("{id:guid}/fulfillment")] public Task<OrderDetailDto> Fulfill(Guid id, FulfillmentUpdateDto dto, CancellationToken ct) => orders.FulfillAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/quantities")] public Task<AdminOrderDetailDto> Quantities(Guid id, UpdateAdminOrderQuantitiesDto dto, CancellationToken ct) => admin.UpdateQuantitiesAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/substitute")] public Task<AdminOrderDetailDto> Substitute(Guid id, SubstituteOrderProductDto dto, CancellationToken ct) => admin.SubstituteAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/split")] public Task<AdminOrderDetailDto> Split(Guid id, SplitOrderShipmentsDto dto, CancellationToken ct) => admin.SplitAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/assign")] public Task<AdminOrderDetailDto> Assign(Guid id, AssignAdminOrderDto dto, CancellationToken ct) => admin.AssignAsync(UserId, id, dto.StaffUserId, ct);
    [HttpPost("{id:guid}/notes")] public Task<AdminOrderNoteDto> Note(Guid id, AddAdminOrderNoteDto dto, CancellationToken ct) => admin.AddNoteAsync(UserId, id, dto.Body, ct);
    [HttpPost("{id:guid}/communications")] public Task<AdminOrderCommunicationDto> Communication(Guid id, AddOrderCommunicationDto dto, CancellationToken ct) => admin.AddCommunicationAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/invoice")] public Task<AdminOrderInvoiceDto> Invoice(Guid id, CancellationToken ct) => admin.IssueInvoiceAsync(id, ct);
    [HttpPost("{id:guid}/cancel")] public Task<AdminOrderDetailDto> Cancel(Guid id, AdminCancelOrderDto dto, CancellationToken ct) => admin.CancelAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/refund")] public Task<AdminOrderRefundDto> Refund(Guid id, ProcessAdminRefundDto dto, CancellationToken ct) => admin.RefundAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct) { await admin.ArchiveAsync(UserId, id, false, ct); return NoContent(); }
    [HttpPost("{id:guid}/restore")] public async Task<IActionResult> Restore(Guid id, CancellationToken ct) { await admin.ArchiveAsync(UserId, id, true, ct); return NoContent(); }
    [HttpGet("{id:guid}/picking")] public Task<List<PickingItemDto>> Picking(Guid id, CancellationToken ct) => admin.PickingAsync(id, ct);
    [HttpGet("{id:guid}/packing")] public Task<List<PackingPackageDto>> Packing(Guid id, CancellationToken ct) => admin.PackingAsync(id, ct);
    [HttpGet("recurring")] public Task<List<AdminRecurringDto>> Recurring(CancellationToken ct) => admin.RecurringAsync(ct);
    [HttpGet("staff")] public Task<List<AdminStaffOptionDto>> Staff(CancellationToken ct) => admin.StaffAsync(ct);
    [HttpPost("recurring/{id:guid}")] public async Task<IActionResult> RecurringUpdate(Guid id, UpdateRecurringAdminDto dto, CancellationToken ct) { await admin.UpdateRecurringAsync(id, dto, ct); return NoContent(); }
}
