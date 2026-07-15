using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminSystemAccess;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/system-access"), Authorize(Roles = "super_admin,system_admin")]
public sealed class AdminSystemAccessController(AdminSystemAccessService access) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet] public Task<SystemAccessDashboardDto> Dashboard(CancellationToken ct) => access.DashboardAsync(UserId, ct);
    [HttpPost("users")] public async Task<IActionResult> CreateUser(SaveSystemUserDto dto, CancellationToken ct) => Ok(new { id = await access.CreateUserAsync(UserId, Ip, dto, ct) });
    [HttpPut("users/{id:guid}")] public async Task<IActionResult> UpdateUser(Guid id, SaveSystemUserDto dto, CancellationToken ct) { await access.UpdateUserAsync(UserId, Ip, id, dto, ct); return NoContent(); }
    [HttpPost("users/{id:guid}/suspend")] public async Task<IActionResult> Suspend(Guid id, SuspendSystemUserDto dto, CancellationToken ct) { await access.SuspendAsync(UserId, Ip, id, dto, ct); return NoContent(); }
    [HttpPost("users/{id:guid}/resume")] public async Task<IActionResult> Resume(Guid id, CancellationToken ct) { await access.ResumeAsync(UserId, Ip, id, ct); return NoContent(); }
    [HttpPost("users/{id:guid}/reset-password")] public async Task<IActionResult> ResetPassword(Guid id, AdminResetPasswordDto dto, CancellationToken ct) { await access.ResetPasswordAsync(UserId, Ip, id, dto, ct); return NoContent(); }
    [HttpPut("users/{id:guid}/scopes")] public async Task<IActionResult> Scopes(Guid id, SaveUserScopesDto dto, CancellationToken ct) { await access.SaveScopesAsync(UserId, Ip, id, dto, ct); return NoContent(); }
    [HttpPost("roles")] public async Task<IActionResult> CreateRole(SaveSystemRoleDto dto, CancellationToken ct) => Ok(new { id = await access.CreateRoleAsync(UserId, Ip, dto, ct) });
    [HttpPut("roles/{id:guid}")] public async Task<IActionResult> UpdateRole(Guid id, SaveSystemRoleDto dto, CancellationToken ct) { await access.UpdateRoleAsync(UserId, Ip, id, dto, ct); return NoContent(); }
    [HttpDelete("sessions/{id:guid}")] public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct) { await access.RevokeSessionAsync(UserId, Ip, id, ct); return NoContent(); }
    [HttpGet("audit/{id:long}")] public Task<SystemAuditDetailDto> AuditDetail(long id, CancellationToken ct) => access.AuditDetailAsync(id, ct);
}
