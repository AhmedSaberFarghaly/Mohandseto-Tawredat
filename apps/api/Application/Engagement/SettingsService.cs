using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Engagement;

public sealed class SettingsService(AppDbContext db, ITenantProvider tenantProvider, OtpService otp)
{
    public async Task<UserSettingsDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await User(userId, ct); var sessions = await db.RefreshTokens.AsNoTracking().Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow).OrderByDescending(t => t.CreatedAt).Select(t => new UserSessionDto(t.Id, t.Device ?? "جهاز غير معروف", t.CreatedAt, t.ExpiresAt, false)).ToListAsync(ct);
        var deletion = await db.AccountDeletionRequests.AsNoTracking().Where(r => r.UserId == userId && r.Status == AccountDeletionStatus.Requested).Select(r => (DateTime?)r.ScheduledFor).SingleOrDefaultAsync(ct);
        return new(user.PreferredLanguage, user.PreferredTheme, user.TwoFactorEnabled, user.TwoFactorChannel, sessions, deletion);
    }

    public async Task<UserSettingsDto> UpdateAppearanceAsync(Guid userId, UpdateAppearanceDto dto, CancellationToken ct = default)
    {
        var user = await User(userId, ct); if (dto.Language is not ("ar" or "en")) throw ApiException.BadRequest("اللغة غير صالحة"); if (dto.Theme is not ("system" or "light" or "dark")) throw ApiException.BadRequest("المظهر غير صالح"); user.PreferredLanguage = dto.Language; user.PreferredTheme = dto.Theme; Audit(userId, "settings.appearance_updated", user.Id); await db.SaveChangesAsync(ct); return await GetAsync(userId, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await User(userId, ct); if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash)) throw ApiException.BadRequest("كلمة المرور الحالية غير صحيحة"); if (dto.NewPassword.Length < 8 || !dto.NewPassword.Any(char.IsUpper) || !dto.NewPassword.Any(char.IsDigit)) throw ApiException.BadRequest("كلمة المرور الجديدة يجب أن تكون 8 أحرف وتضم رقمًا وحرفًا كبيرًا");
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash)) throw ApiException.BadRequest("اختر كلمة مرور مختلفة"); user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword); await RevokeAll(userId, ct); Audit(userId, "settings.password_changed", user.Id); await db.SaveChangesAsync(ct);
    }

    public async Task<string?> RequestTwoFactorAsync(Guid userId, TwoFactorRequestDto dto, CancellationToken ct = default)
    {
        if (!string.Equals(dto.Channel, "sms", StringComparison.OrdinalIgnoreCase)) throw ApiException.BadRequest("القناة المتاحة حاليًا هي الرسائل النصية"); var user = await User(userId, ct); if (user.TwoFactorEnabled) throw ApiException.Conflict("المصادقة الثنائية مفعلة بالفعل"); return await otp.RequestAsync(user.Phone, OtpPurpose.TwoFactor, ct);
    }

    public async Task EnableTwoFactorAsync(Guid userId, TwoFactorVerifyDto dto, CancellationToken ct = default)
    {
        var user = await User(userId, ct); await otp.VerifyAsync(user.Phone, dto.Code, OtpPurpose.TwoFactor, ct); user.TwoFactorEnabled = true; user.TwoFactorChannel = "sms"; Audit(userId, "settings.two_factor_enabled", user.Id); await db.SaveChangesAsync(ct);
    }

    public async Task DisableTwoFactorAsync(Guid userId, DisableTwoFactorDto dto, CancellationToken ct = default)
    {
        var user = await User(userId, ct); if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) throw ApiException.BadRequest("كلمة المرور غير صحيحة"); user.TwoFactorEnabled = false; user.TwoFactorChannel = null; Audit(userId, "settings.two_factor_disabled", user.Id); await db.SaveChangesAsync(ct);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId && t.RevokedAt == null, ct) ?? throw ApiException.NotFound("الجلسة غير موجودة"); token.RevokedAt = DateTime.UtcNow; Audit(userId, "settings.session_revoked", token.Id); await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default) { await RevokeAll(userId, ct); Audit(userId, "settings.all_sessions_revoked", userId); await db.SaveChangesAsync(ct); }

    public async Task<DateTime> RequestDeletionAsync(Guid userId, DeleteAccountDto dto, CancellationToken ct = default)
    {
        var user = await User(userId, ct); if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) throw ApiException.BadRequest("كلمة المرور غير صحيحة"); if (string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("سبب حذف الحساب مطلوب");
        if (await db.AccountDeletionRequests.AnyAsync(r => r.UserId == userId && r.Status == AccountDeletionStatus.Requested, ct)) throw ApiException.Conflict("يوجد طلب حذف قائم بالفعل"); var scheduled = DateTime.UtcNow.AddDays(30);
        db.AccountDeletionRequests.Add(new AccountDeletionRequest { TenantId = TenantId(), UserId = userId, Reason = dto.Reason.Trim()[..Math.Min(dto.Reason.Trim().Length, 1000)], ScheduledFor = scheduled }); await RevokeAll(userId, ct); Audit(userId, "settings.account_deletion_requested", userId); await db.SaveChangesAsync(ct); return scheduled;
    }

    public async Task CancelDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var request = await db.AccountDeletionRequests.FirstOrDefaultAsync(r => r.UserId == userId && r.Status == AccountDeletionStatus.Requested, ct) ?? throw ApiException.NotFound("لا يوجد طلب حذف قائم"); request.Status = AccountDeletionStatus.Cancelled; request.CancelledAt = DateTime.UtcNow; Audit(userId, "settings.account_deletion_cancelled", userId); await db.SaveChangesAsync(ct);
    }

    private async Task<User> User(Guid id, CancellationToken ct) => await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId(), ct) ?? throw ApiException.NotFound("المستخدم غير موجود");
    private async Task RevokeAll(Guid userId, CancellationToken ct) { foreach (var token in await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct)) token.RevokedAt = DateTime.UtcNow; }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("هذا الحساب غير مرتبط بشركة");
    private void Audit(Guid userId, string action, Guid entityId) => db.AuditLogs.Add(new AuditLog { TenantId = TenantId(), UserId = userId, Action = action, EntityType = nameof(User), EntityId = entityId.ToString() });
}
