using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminCustomerService;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize(Roles = "super_admin,support_agent,operations_manager,warehouse_manager"), Route("api/admin/customer-service")]
public sealed class AdminCustomerServiceController(AdminCustomerServiceService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<CustomerServiceDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);
    [HttpPost("tickets/{id:guid}/assign")] public async Task<IActionResult> Assign(Guid id, AssignSupportStaffDto dto, CancellationToken ct) { await service.AssignAsync(id, dto, ct); return NoContent(); }
    [HttpPost("tickets/{id:guid}/reply")] public async Task<IActionResult> Reply(Guid id, ReplySupportTicketDto dto, CancellationToken ct) { await service.ReplyAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("tickets/{id:guid}/escalate")] public async Task<IActionResult> Escalate(Guid id, EscalateSupportTicketDto dto, CancellationToken ct) { await service.EscalateAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPut("tickets/{id:guid}/status")] public async Task<IActionResult> Status(Guid id, ChangeSupportStatusDto dto, CancellationToken ct) { await service.ChangeStatusAsync(id, dto, ct); return NoContent(); }
    [HttpPost("sla")] public Task<SupportSlaDto> Sla(SaveSlaPolicyDto dto, CancellationToken ct) => service.SaveSlaAsync(null, dto, ct);
    [HttpPut("sla/{id:guid}")] public Task<SupportSlaDto> Sla(Guid id, SaveSlaPolicyDto dto, CancellationToken ct) => service.SaveSlaAsync(id, dto, ct);
    [HttpPost("templates")] public Task<SupportTemplateDto> Template(SaveReplyTemplateDto dto, CancellationToken ct) => service.SaveTemplateAsync(null, dto, ct);
    [HttpPut("templates/{id:guid}")] public Task<SupportTemplateDto> Template(Guid id, SaveReplyTemplateDto dto, CancellationToken ct) => service.SaveTemplateAsync(id, dto, ct);
    [HttpPost("returns/{id:guid}/items/{itemId:guid}/disposition")] public async Task<IActionResult> Disposition(Guid id, Guid itemId, ReturnDispositionDto dto, CancellationToken ct) { await service.SetDispositionAsync(UserId, id, itemId, dto, ct); return NoContent(); }
    [HttpGet("returns/attachments/{id:guid}")] public async Task<IActionResult> ReturnFile(Guid id, CancellationToken ct) { var file = await service.ReturnFileAsync(id, ct); return PhysicalFile(file.Path, file.Type, file.Name, true); }
    [HttpGet("tickets/attachments/{id:guid}")] public async Task<IActionResult> TicketFile(Guid id, CancellationToken ct) { var file = await service.SupportFileAsync(id, ct); return PhysicalFile(file.Path, file.Type, file.Name, true); }
}
