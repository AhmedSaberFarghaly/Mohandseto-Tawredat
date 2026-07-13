using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Orders;

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

[ApiController, Authorize(Roles = "platform_admin,operations_manager,warehouse_manager,delivery_agent"), Route("api/admin/orders")]
public sealed class AdminOrdersController(OrderService orders) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPost("{id:guid}/fulfillment")] public Task<OrderDetailDto> Fulfill(Guid id, FulfillmentUpdateDto dto, CancellationToken ct) => orders.FulfillAsync(UserId, id, dto, ct);
}
