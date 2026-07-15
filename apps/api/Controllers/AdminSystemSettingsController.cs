using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/system-settings"), Authorize(Roles = "super_admin,system_admin")]
public sealed class AdminSystemSettingsController(AdminSystemSettingsService settings) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet] public Task<SystemSettingsDashboardDto> Dashboard(CancellationToken ct) => settings.DashboardAsync(ct);
    [HttpPut("sections/{code}")] public Task<SettingSectionDto> Section(string code, SaveSettingsSectionDto dto, CancellationToken ct) => settings.SaveSectionAsync(UserId, Ip, code, dto, ct);

    [HttpPost("bank-accounts")] public Task<SettingsBankAccountDto> CreateBank(SaveSettingsBankAccountDto dto, CancellationToken ct) => settings.SaveBankAsync(UserId, Ip, null, dto, ct);
    [HttpPut("bank-accounts/{id:guid}")] public Task<SettingsBankAccountDto> UpdateBank(Guid id, SaveSettingsBankAccountDto dto, CancellationToken ct) => settings.SaveBankAsync(UserId, Ip, id, dto, ct);
    [HttpDelete("bank-accounts/{id:guid}")] public async Task<IActionResult> DeleteBank(Guid id, CancellationToken ct) { await settings.DeleteBankAsync(UserId, Ip, id, ct); return NoContent(); }

    [HttpPost("delivery-zones")] public Task<SettingsDeliveryZoneDto> CreateZone(SaveSettingsDeliveryZoneDto dto, CancellationToken ct) => settings.SaveZoneAsync(UserId, Ip, null, dto, ct);
    [HttpPut("delivery-zones/{id:guid}")] public Task<SettingsDeliveryZoneDto> UpdateZone(Guid id, SaveSettingsDeliveryZoneDto dto, CancellationToken ct) => settings.SaveZoneAsync(UserId, Ip, id, dto, ct);
    [HttpDelete("delivery-zones/{id:guid}")] public async Task<IActionResult> DeleteZone(Guid id, CancellationToken ct) { await settings.DeleteZoneAsync(UserId, Ip, id, ct); return NoContent(); }

    [HttpPost("api-keys")] public Task<SettingsApiKeyDto> CreateApiKey(CreateSettingsApiKeyDto dto, CancellationToken ct) => settings.CreateApiKeyAsync(UserId, Ip, dto, ct);
    [HttpDelete("api-keys/{id:guid}")] public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken ct) { await settings.RevokeApiKeyAsync(UserId, Ip, id, ct); return NoContent(); }

    [HttpPost("webhooks")] public Task<SettingsWebhookDto> CreateWebhook(SaveSettingsWebhookDto dto, CancellationToken ct) => settings.SaveWebhookAsync(UserId, Ip, null, dto, ct);
    [HttpPut("webhooks/{id:guid}")] public Task<SettingsWebhookDto> UpdateWebhook(Guid id, SaveSettingsWebhookDto dto, CancellationToken ct) => settings.SaveWebhookAsync(UserId, Ip, id, dto, ct);
    [HttpDelete("webhooks/{id:guid}")] public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken ct) { await settings.DeleteWebhookAsync(UserId, Ip, id, ct); return NoContent(); }

    [HttpPost("translations")] public Task<SettingsTranslationDto> Translation(SaveSettingsTranslationDto dto, CancellationToken ct) => settings.SaveTranslationAsync(UserId, Ip, dto, ct);
    [HttpPost("backups")] public Task<SettingsBackupDto> Backup(CancellationToken ct) => settings.CreateBackupAsync(UserId, Ip, false, ct);
}
