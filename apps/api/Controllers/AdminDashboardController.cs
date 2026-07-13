using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Admin;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Authorize(Roles = "super_admin,platform_admin,operations_manager,sales_manager")]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(AdminDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<AdminDashboardDto> Get([FromQuery] int days = 7, CancellationToken ct = default) =>
        dashboard.GetAsync(days, ct);
}
