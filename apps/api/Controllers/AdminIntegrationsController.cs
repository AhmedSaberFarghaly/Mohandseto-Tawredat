using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminIntegrations;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController,Route("api/admin/integrations"),Authorize(Roles="super_admin,system_admin")]
public sealed class AdminIntegrationsController(AdminIntegrationService integrations):ControllerBase
{
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out var id)?id:throw ApiException.Unauthorized();
    private string? Ip=>HttpContext.Connection.RemoteIpAddress?.ToString();
    [HttpGet]public Task<IntegrationDashboardDto> Dashboard(CancellationToken ct)=>integrations.DashboardAsync(ct);
    [HttpGet("connections/{code}")]public Task<IntegrationDetailDto> Detail(string code,CancellationToken ct)=>integrations.DetailAsync(code,ct);
    [HttpPut("connections/{code}")]public Task<IntegrationDetailDto> Save(string code,SaveIntegrationConnectionDto dto,CancellationToken ct)=>integrations.SaveAsync(UserId,Ip,code,dto,ct);
    [HttpPost("connections/{code}/test")]public Task<IntegrationActionResultDto> Test(string code,CancellationToken ct)=>integrations.TestAsync(UserId,Ip,code,ct);
    [HttpPost("connections/{code}/run")]public Task<IntegrationActionResultDto> Run(string code,RunIntegrationOperationDto dto,CancellationToken ct)=>integrations.RunAsync(UserId,Ip,code,dto,ct);
    [HttpPost("connections/{code}/disable")]public async Task<IActionResult> Disable(string code,CancellationToken ct){await integrations.DisableAsync(UserId,Ip,code,ct);return NoContent();}
    [HttpGet("operations")]public Task<IntegrationOperationsPageDto> Operations([FromQuery]string? search,[FromQuery]string? integration,[FromQuery]string? status,[FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=25,CancellationToken ct=default)=>integrations.OperationsAsync(search,integration,status,from,to,page,pageSize,ct);
    [HttpGet("operations/{id:guid}")]public Task<IntegrationOperationDto> Operation(Guid id,CancellationToken ct)=>integrations.OperationAsync(id,ct);
    [HttpPost("operations/{id:guid}/retry")]public Task<IntegrationActionResultDto> Retry(Guid id,CancellationToken ct)=>integrations.RetryAsync(UserId,Ip,id,ct);
    [HttpPost("operations/retry-all")]public Task<int> RetryAll(CancellationToken ct)=>integrations.RetryAllAsync(UserId,Ip,ct);
}
