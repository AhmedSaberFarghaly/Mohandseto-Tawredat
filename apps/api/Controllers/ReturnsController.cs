using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Returns;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/returns")]
public sealed class ReturnsController(ReturnService returns) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("eligible-orders")] public Task<List<EligibleReturnOrderDto>> Eligible(CancellationToken ct) => returns.EligibleOrdersAsync(UserId, ct);
    [HttpGet] public Task<List<ReturnListDto>> List([FromQuery] string? status, CancellationToken ct) => returns.ListAsync(UserId, status, ct);
    [HttpPost] public Task<ReturnDetailDto> Create(CreateReturnDto dto, CancellationToken ct) => returns.CreateAsync(UserId, dto, ct);
    [HttpGet("{id:guid}")] public Task<ReturnDetailDto> Detail(Guid id, CancellationToken ct) => returns.DetailAsync(UserId, id, ct);
    [HttpPost("{id:guid}/attachments"), RequestSizeLimit(10 * 1024 * 1024 + 4096)] public Task<ReturnAttachmentDto> Upload(Guid id, [FromForm] Guid? returnItemId, IFormFile file, CancellationToken ct) => returns.UploadAsync(UserId, id, returnItemId, file, ct);
    [HttpGet("attachments/{id:guid}")] public async Task<IActionResult> File(Guid id, CancellationToken ct) { var file = await returns.FileAsync(UserId, id, ct); return PhysicalFile(file.Path, file.Type, file.Name, enableRangeProcessing: true); }
    [HttpPost("{id:guid}/submit")] public Task<ReturnDetailDto> Submit(Guid id, CancellationToken ct) => returns.SubmitAsync(UserId, id, ct);
    [HttpPost("{id:guid}/cancel")] public Task<ReturnDetailDto> Cancel(Guid id, CancellationToken ct) => returns.CancelAsync(UserId, id, ct);
}

[ApiController, Authorize(Roles = "super_admin,support_agent,warehouse_manager,operations_manager,delivery_driver"), Route("api/admin/returns")]
public sealed class AdminReturnsController(ReturnService returns) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPost("{id:guid}/{decision:regex(^(review|approve|reject)$)}")] public Task<ReturnDetailDto> Decision(Guid id, string decision, ReturnDecisionDto dto, CancellationToken ct) => returns.DecisionAsync(UserId, id, decision, dto, ct);
    [HttpPost("{id:guid}/pickup")] public Task<ReturnDetailDto> Pickup(Guid id, ReturnPickupDto dto, CancellationToken ct) => returns.SchedulePickupAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/tracking")] public Task<ReturnDetailDto> Tracking(Guid id, ReturnTrackingDto dto, CancellationToken ct) => returns.TrackPickupAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/inspection")] public Task<ReturnDetailDto> Inspection(Guid id, ReturnInspectionDto dto, CancellationToken ct) => returns.InspectAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/progress")] public Task<ReturnDetailDto> Progress(Guid id, ReturnProgressDto dto, CancellationToken ct) => returns.ProgressAsync(UserId, id, dto, ct);
}
