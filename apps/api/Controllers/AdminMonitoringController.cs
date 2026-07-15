using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminMonitoring;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/monitoring"), Authorize(Roles = "super_admin,system_admin")]
public sealed class AdminMonitoringController(AdminMonitoringService monitoring) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet] public Task<MonitoringDashboardDto> Dashboard(CancellationToken ct) => monitoring.DashboardAsync(ct);
    [HttpGet("errors")] public Task<ErrorPageDto> Errors([FromQuery] string? search, [FromQuery] string? severity,
        [FromQuery] string? service, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        monitoring.ErrorsAsync(search, severity, service, from, to, page, pageSize, ct);
    [HttpGet("errors/{id:guid}")] public Task<ErrorEventDto> Error(Guid id, CancellationToken ct) => monitoring.ErrorAsync(id, ct);
    [HttpPost("errors/{id:guid}/resolve")] public Task<ErrorEventDto> ResolveError(Guid id, ResolveErrorDto dto, CancellationToken ct) => monitoring.ResolveErrorAsync(UserId, Ip, id, dto, ct);

    [HttpPost("security/blocked-ips"), Authorize(Roles = "super_admin")] public Task<BlockedIpDto> BlockIp(BlockIpDto dto, CancellationToken ct) => monitoring.BlockIpAsync(UserId, Ip, dto, ct);
    [HttpDelete("security/blocked-ips/{id:guid}"), Authorize(Roles = "super_admin")] public async Task<IActionResult> UnblockIp(Guid id, CancellationToken ct) { await monitoring.UnblockIpAsync(UserId, Ip, id, ct); return NoContent(); }
    [HttpPost("security/activities/{id:guid}/investigate")] public Task<SuspiciousActivityDto> Investigate(Guid id, ReviewSuspiciousActivityDto dto, CancellationToken ct) => monitoring.InvestigateAsync(UserId, Ip, id, dto, ct);
    [HttpPost("security/activities/{id:guid}/ignore")] public Task<SuspiciousActivityDto> Ignore(Guid id, ReviewSuspiciousActivityDto dto, CancellationToken ct) => monitoring.IgnoreAsync(UserId, Ip, id, dto, ct);

    [HttpPost("backups"), Authorize(Roles = "super_admin")] public Task<SettingsBackupDto> CreateBackup(CancellationToken ct) => monitoring.CreateBackupAsync(UserId, Ip, ct);
    [HttpPost("backups/{id:guid}/restore-requests"), Authorize(Roles = "super_admin")] public Task<RestoreRequestDto> RequestRestore(Guid id, CreateRestoreRequestDto dto, CancellationToken ct) => monitoring.RequestRestoreAsync(UserId, Ip, id, dto, ct);

    [HttpPost("versions"), Authorize(Roles = "super_admin")] public Task<SystemVersionDto> SaveVersion(SaveSystemVersionDto dto, CancellationToken ct) => monitoring.SaveVersionAsync(UserId, Ip, dto, ct);
    [HttpPost("feature-flags"), Authorize(Roles = "super_admin")] public Task<FeatureFlagDto> CreateFlag(SaveFeatureFlagDto dto, CancellationToken ct) => monitoring.SaveFlagAsync(UserId, Ip, null, dto, ct);
    [HttpPut("feature-flags/{id:guid}"), Authorize(Roles = "super_admin")] public Task<FeatureFlagDto> UpdateFlag(Guid id, SaveFeatureFlagDto dto, CancellationToken ct) => monitoring.SaveFlagAsync(UserId, Ip, id, dto, ct);
    [HttpDelete("feature-flags/{id:guid}"), Authorize(Roles = "super_admin")] public async Task<IActionResult> DeleteFlag(Guid id, CancellationToken ct) { await monitoring.DeleteFlagAsync(UserId, Ip, id, ct); return NoContent(); }
}

[ApiController, Route("api/features"), Authorize]
public sealed class FeatureFlagsController(AdminMonitoringService monitoring) : ControllerBase
{
    [HttpGet]
    public Task<EvaluatedFeatureFlagsDto> Evaluate(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)) throw ApiException.Unauthorized();
        var tenantId = Guid.TryParse(User.FindFirstValue("tenant_id"), out var tenant) ? tenant : (Guid?)null;
        return monitoring.EvaluateFlagsAsync(userId, tenantId, ct);
    }
}
