using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminCrm;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/crm"), Authorize(Roles = "super_admin,system_admin,crm_manager,sales_manager,sales_agent,operations_manager")]
public sealed class AdminCrmController(AdminCrmService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<CrmDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);
    [HttpGet("companies/{id:guid}")] public Task<CrmCompanyDetailDto> Detail(Guid id, CancellationToken ct) => service.DetailAsync(id, ct);
    [HttpPost("companies")] public async Task<IActionResult> Create(SaveCrmCompanyDto dto, CancellationToken ct) => Ok(new { id = await service.CreateCompanyAsync(UserId, dto, ct) });
    [HttpPut("companies/{id:guid}")] public async Task<IActionResult> Update(Guid id, SaveCrmCompanyDto dto, CancellationToken ct) { await service.UpdateCompanyAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/stage")] public async Task<IActionResult> Stage(Guid id, ChangeCustomerStageDto dto, CancellationToken ct) { await service.ChangeStageAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/activities")] public async Task<IActionResult> Activity(Guid id, AddCrmActivityDto dto, CancellationToken ct) { await service.AddActivityAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/tasks")] public async Task<IActionResult> Task(Guid id, AddCrmTaskDto dto, CancellationToken ct) { await service.AddTaskAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("tasks/{id:guid}/complete")] public async Task<IActionResult> Complete(Guid id, CancellationToken ct) { await service.CompleteTaskAsync(UserId, id, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/branches")] public async Task<IActionResult> Branch(Guid id, AddCrmBranchDto dto, CancellationToken ct) { await service.AddBranchAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/documents/{documentId:guid}/review")] public async Task<IActionResult> Review(Guid id, Guid documentId, ReviewCrmDocumentDto dto, CancellationToken ct) { await service.ReviewDocumentAsync(UserId, id, documentId, dto, ct); return NoContent(); }
    [HttpPut("companies/{id:guid}/prices")] public async Task<IActionResult> Prices(Guid id, ReplaceCrmPricesDto dto, CancellationToken ct) { await service.ReplacePricesAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPut("companies/{id:guid}/credit-limit")] public async Task<IActionResult> Credit(Guid id, UpdateCreditLimitDto dto, CancellationToken ct) { await service.UpdateCreditAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("companies/{id:guid}/status")] public async Task<IActionResult> Status(Guid id, ChangeCompanyStatusDto dto, CancellationToken ct) { await service.ChangeStatusAsync(UserId, id, dto, ct); return NoContent(); }
}
