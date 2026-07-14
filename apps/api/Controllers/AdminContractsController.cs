using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminContracts;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/contracts"), Authorize(Roles = "super_admin,system_admin,contract_manager,sales_manager,finance_manager,operations_manager")]
public sealed class AdminContractsController(AdminContractService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<ContractDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);
    [HttpGet("{id:guid}")] public Task<ContractDetailDto> Detail(Guid id, CancellationToken ct) => service.DetailAsync(id, ct);
    [HttpPost] public async Task<IActionResult> Create(SaveContractDto dto, CancellationToken ct) => Ok(new { id = await service.CreateAsync(UserId, dto, ct) });
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, SaveContractDto dto, CancellationToken ct) { await service.UpdateDraftAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/attachments")] public async Task<IActionResult> Attachment(Guid id, AddContractAttachmentDto dto, CancellationToken ct) { await service.AddAttachmentAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/approvals/{approvalId:guid}/decide")] public async Task<IActionResult> Decide(Guid id, Guid approvalId, DecideContractApprovalDto dto, CancellationToken ct) { await service.DecideApprovalAsync(UserId, id, approvalId, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/activate")] public async Task<IActionResult> Activate(Guid id, ActivateContractDto dto, CancellationToken ct) { await service.ActivateAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/renew")] public async Task<IActionResult> Renew(Guid id, RenewContractDto dto, CancellationToken ct) { await service.RenewAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("{id:guid}/price-revisions")] public async Task<IActionResult> Revision(Guid id, CreateContractPriceRevisionDto dto, CancellationToken ct) { await service.CreatePriceRevisionAsync(UserId, id, dto, ct); return NoContent(); }
}
