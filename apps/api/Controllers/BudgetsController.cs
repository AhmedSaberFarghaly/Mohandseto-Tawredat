using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Budgets;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/budgets")]
public sealed class BudgetsController(BudgetService budgets) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("summary")] public Task<BudgetSummaryDto> Summary([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct) => budgets.SummaryAsync(year, month, ct);
    [HttpGet("centers/{id:guid}")] public Task<BudgetCenterDetailDto> Center(Guid id, CancellationToken ct) => budgets.CenterAsync(id, ct);
    [HttpGet("alerts")] public Task<List<BudgetAlertDto>> Alerts(CancellationToken ct) => budgets.AlertsAsync(ct);
    [HttpPost("adjustment-requests")] public Task<BudgetAdjustmentDto> Adjust(CreateBudgetAdjustmentDto dto, CancellationToken ct) => budgets.RequestAdjustmentAsync(UserId, dto, ct);
}

[ApiController, Authorize(Roles = "platform_admin,finance_manager,budget_manager"), Route("api/admin/budgets")]
public sealed class AdminBudgetsController(BudgetService budgets) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPost("adjustment-requests/{id:guid}/decision")] public Task<BudgetAdjustmentDto> Decide(Guid id, BudgetAdjustmentDecisionDto dto, CancellationToken ct) => budgets.DecideAsync(UserId, id, dto, ct);
}
