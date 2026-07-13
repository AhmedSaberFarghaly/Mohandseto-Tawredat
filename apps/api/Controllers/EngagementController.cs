using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Engagement;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<NotificationPageDto> List([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] bool unreadOnly = false, CancellationToken ct = default) => notifications.ListAsync(UserId, page, pageSize, unreadOnly, ct);
    [HttpPost("{id:guid}/read")] public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await notifications.MarkReadAsync(UserId, id, ct); return NoContent(); }
    [HttpPost("read-all")] public async Task<IActionResult> ReadAll(CancellationToken ct) { await notifications.MarkReadAsync(UserId, null, ct); return NoContent(); }
    [HttpGet("preferences")] public Task<NotificationPreferencesDto> Preferences(CancellationToken ct) => notifications.PreferencesAsync(UserId, ct);
    [HttpPut("preferences")] public Task<NotificationPreferencesDto> Preferences(NotificationPreferencesDto dto, CancellationToken ct) => notifications.UpdatePreferencesAsync(UserId, dto, ct);
}

[ApiController, Authorize, Route("api/support")]
public sealed class SupportController(SupportService support, AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("tickets")] public Task<List<SupportTicketListDto>> Tickets(CancellationToken ct) => support.ListAsync(UserId, ct);
    [HttpGet("tickets/{id:guid}")] public Task<SupportTicketDetailDto> Ticket(Guid id, CancellationToken ct) => support.DetailAsync(UserId, id, ct);
    [HttpPost("tickets"), RequestSizeLimit(52_500_000)] public Task<SupportTicketDetailDto> Create([FromForm] CreateSupportTicketDto dto, [FromForm] List<IFormFile> files, CancellationToken ct) => support.CreateAsync(UserId, dto, files, ct);
    [HttpPost("tickets/{id:guid}/messages"), RequestSizeLimit(31_500_000)] public Task<SupportMessageDto> Message(Guid id, [FromForm] string body, [FromForm] List<IFormFile> files, CancellationToken ct) => support.AddMessageAsync(UserId, id, new(body), files, ct);
    [HttpPost("tickets/{id:guid}/close")] public async Task<IActionResult> Close(Guid id, CancellationToken ct) { await support.CloseAsync(UserId, id, ct); return NoContent(); }
    [HttpPost("tickets/{id:guid}/rating")] public async Task<IActionResult> Rate(Guid id, RateSupportTicketDto dto, CancellationToken ct) { await support.RateAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("callback-requests")] public Task<CallbackRequestDto> Callback(CreateCallbackRequestDto dto, CancellationToken ct) => support.CallbackAsync(UserId, dto, ct);
    [HttpGet("attachments/{id:guid}")] public async Task<IActionResult> Attachment(Guid id, CancellationToken ct)
    {
        var a = await db.SupportAttachments.AsNoTracking().Include(a => a.Ticket).FirstOrDefaultAsync(a => a.Id == id && a.Ticket.UserId == UserId, ct) ?? throw ApiException.NotFound("المرفق غير موجود"); var root = Path.GetFullPath(env.ContentRootPath); var full = Path.GetFullPath(Path.Combine(root, a.StoredPath));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(full)) throw ApiException.NotFound("ملف المرفق غير موجود"); return PhysicalFile(full, a.ContentType, a.OriginalName, true);
    }
}

[ApiController, AllowAnonymous, Route("api/content")]
public sealed class PublicContentController(SupportService support) : ControllerBase
{
    [HttpGet("faq")] public Task<List<SupportArticleDto>> Faq([FromQuery] string? category, CancellationToken ct) => support.ArticlesAsync(category, ct);
    [HttpGet("pages/{slug}")] public Task<ContentPageDto> Page(string slug, CancellationToken ct) => support.PageAsync(slug, ct);
}

[ApiController, AllowAnonymous, Route("api/app")]
public sealed class AppRuntimeController(AppDbContext db) : ControllerBase
{
    [HttpGet("config")]
    public async Task<MobileAppConfigDto> Config([FromQuery] string platform = "all", CancellationToken ct = default)
    {
        var config = await db.MobileAppConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Platform == platform, ct) ?? await db.MobileAppConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Platform == "all", ct) ?? throw ApiException.NotFound("إعدادات التطبيق غير متاحة");
        return new(config.MinimumVersion, config.LatestVersion, config.MaintenanceEnabled, config.MessageAr, config.UpdateUrl);
    }
}

[ApiController, Authorize, Route("api/settings")]
public sealed class SettingsController(SettingsService settings) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<UserSettingsDto> Get(CancellationToken ct) => settings.GetAsync(UserId, ct);
    [HttpPut("appearance")] public Task<UserSettingsDto> Appearance(UpdateAppearanceDto dto, CancellationToken ct) => settings.UpdateAppearanceAsync(UserId, dto, ct);
    [HttpPost("password")] public async Task<IActionResult> Password(ChangePasswordDto dto, CancellationToken ct) { await settings.ChangePasswordAsync(UserId, dto, ct); return NoContent(); }
    [HttpPost("two-factor/request")] public async Task<IActionResult> RequestTwoFactor(TwoFactorRequestDto dto, CancellationToken ct) { var devCode = await settings.RequestTwoFactorAsync(UserId, dto, ct); return Accepted(new { sent = true, devCode }); }
    [HttpPost("two-factor/enable")] public async Task<IActionResult> EnableTwoFactor(TwoFactorVerifyDto dto, CancellationToken ct) { await settings.EnableTwoFactorAsync(UserId, dto, ct); return NoContent(); }
    [HttpPost("two-factor/disable")] public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorDto dto, CancellationToken ct) { await settings.DisableTwoFactorAsync(UserId, dto, ct); return NoContent(); }
    [HttpDelete("sessions/{id:guid}")] public async Task<IActionResult> Session(Guid id, CancellationToken ct) { await settings.RevokeSessionAsync(UserId, id, ct); return NoContent(); }
    [HttpDelete("sessions")] public async Task<IActionResult> Sessions(CancellationToken ct) { await settings.RevokeAllSessionsAsync(UserId, ct); return NoContent(); }
    [HttpPost("account-deletion")] public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto, CancellationToken ct) => Accepted(new { scheduledFor = await settings.RequestDeletionAsync(UserId, dto, ct) });
    [HttpDelete("account-deletion")] public async Task<IActionResult> CancelDeletion(CancellationToken ct) { await settings.CancelDeletionAsync(UserId, ct); return NoContent(); }
}
