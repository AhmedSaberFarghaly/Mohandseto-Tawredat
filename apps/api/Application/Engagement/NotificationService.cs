using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Engagement;

public sealed class NotificationService(AppDbContext db, ITenantProvider tenantProvider)
{
    public async Task<NotificationPageDto> ListAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId); var unread = await query.CountAsync(n => n.ReadAt == null, ct); if (unreadOnly) query = query.Where(n => n.ReadAt == null);
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.EntityType, n.EntityId, n.ReadAt != null, n.CreatedAt)).ToListAsync(ct);
        return new(unread, total, items);
    }

    public async Task MarkReadAsync(Guid userId, Guid? id, CancellationToken ct = default)
    {
        var query = db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null); if (id is not null) query = query.Where(n => n.Id == id);
        var items = await query.ToListAsync(ct); if (id is not null && items.Count == 0 && !await db.Notifications.AnyAsync(n => n.Id == id && n.UserId == userId, ct)) throw ApiException.NotFound("الإشعار غير موجود");
        foreach (var n in items) n.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(ct);
    }

    public async Task<NotificationPreferencesDto> PreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct); return p is null ? Default() : Map(p);
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto dto, CancellationToken ct = default)
    {
        var p = await db.NotificationPreferences.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? new NotificationPreference { TenantId = TenantId(), UserId = userId };
        if (db.Entry(p).State == EntityState.Detached) db.NotificationPreferences.Add(p);
        p.PushEnabled = dto.PushEnabled; p.EmailEnabled = dto.EmailEnabled; p.SmsEnabled = dto.SmsEnabled; p.OrdersEnabled = dto.OrdersEnabled; p.ApprovalsEnabled = dto.ApprovalsEnabled; p.QuotesEnabled = dto.QuotesEnabled; p.InvoicesEnabled = dto.InvoicesEnabled; p.PromotionsEnabled = dto.PromotionsEnabled;
        await db.SaveChangesAsync(ct); return Map(p);
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("هذا الحساب غير مرتبط بشركة");
    private static NotificationPreferencesDto Default() => new(true, true, false, true, true, true, true, false);
    private static NotificationPreferencesDto Map(NotificationPreference p) => new(p.PushEnabled, p.EmailEnabled, p.SmsEnabled, p.OrdersEnabled, p.ApprovalsEnabled, p.QuotesEnabled, p.InvoicesEnabled, p.PromotionsEnabled);
}
