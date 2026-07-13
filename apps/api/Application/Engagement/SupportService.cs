using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Engagement;

public sealed class SupportService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment env)
{
    private static readonly HashSet<string> AllowedTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileBytes = 10 * 1024 * 1024;

    public async Task<List<SupportTicketListDto>> ListAsync(Guid userId, CancellationToken ct = default) => await db.SupportTickets.AsNoTracking().Where(t => t.UserId == userId).OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).Select(t => new SupportTicketListDto(t.Id, t.Number, t.Type.ToString(), t.Priority.ToString(), t.Status.ToString(), t.Subject, t.CreatedAt, t.UpdatedAt, t.Messages.Count(m => m.IsStaff && m.ReadAt == null))).ToListAsync(ct);

    public async Task<SupportTicketDetailDto> DetailAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var ticket = await db.SupportTickets.Include(t => t.Messages).Include(t => t.Attachments).FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct) ?? throw ApiException.NotFound("تذكرة الدعم غير موجودة");
        foreach (var m in ticket.Messages.Where(m => m.IsStaff && m.ReadAt == null)) m.ReadAt = DateTime.UtcNow; if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
        var senderIds = ticket.Messages.Select(m => m.SenderUserId).Distinct().ToList(); var names = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => senderIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct); return Map(ticket, names);
    }

    public async Task<SupportTicketDetailDto> CreateAsync(Guid userId, CreateSupportTicketDto dto, IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SupportTicketType>(dto.Type, true, out var type)) throw ApiException.BadRequest("نوع المشكلة غير صالح"); if (!Enum.TryParse<SupportTicketPriority>(dto.Priority, true, out var priority)) priority = SupportTicketPriority.Normal;
        if (dto.OrderId is { } oid && !await db.Orders.AnyAsync(o => o.Id == oid && o.UserId == userId, ct)) throw ApiException.BadRequest("الطلب المرتبط غير صالح");
        var ticket = new SupportTicket { TenantId = TenantId(), UserId = userId, Number = $"SUP-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(), Type = type, Priority = priority, Subject = Required(dto.Subject, 200, "عنوان المشكلة مطلوب"), Description = Required(dto.Description, 4000, "تفاصيل المشكلة مطلوبة"), OrderId = dto.OrderId };
        db.SupportTickets.Add(ticket); db.SupportMessages.Add(new SupportMessage { TenantId = TenantId(), TicketId = ticket.Id, SenderUserId = userId, Body = ticket.Description });
        foreach (var file in files.Take(5)) db.SupportAttachments.Add(await Store(ticket.Id, userId, file, ct));
        db.AuditLogs.Add(new AuditLog { TenantId = TenantId(), UserId = userId, Action = "support.ticket_created", EntityType = nameof(SupportTicket), EntityId = ticket.Id.ToString() }); await db.SaveChangesAsync(ct); return await DetailAsync(userId, ticket.Id, ct);
    }

    public async Task<SupportMessageDto> AddMessageAsync(Guid userId, Guid id, AddSupportMessageDto dto, IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct) ?? throw ApiException.NotFound("تذكرة الدعم غير موجودة"); if (ticket.Status is SupportTicketStatus.Closed) throw ApiException.Conflict("التذكرة مغلقة");
        var message = new SupportMessage { TenantId = TenantId(), TicketId = ticket.Id, SenderUserId = userId, Body = Required(dto.Body, 3000, "اكتب الرسالة") }; db.SupportMessages.Add(message);
        foreach (var file in files.Take(3)) db.SupportAttachments.Add(await Store(ticket.Id, userId, file, ct)); if (ticket.Status == SupportTicketStatus.WaitingCustomer) ticket.Status = SupportTicketStatus.InProgress;
        ticket.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); var name = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).SingleAsync(ct); return new(message.Id, userId, name, false, message.Body, message.CreatedAt, null);
    }

    public async Task CloseAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct) ?? throw ApiException.NotFound("تذكرة الدعم غير موجودة"); ticket.Status = SupportTicketStatus.Closed; ticket.ClosedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct);
    }

    public async Task RateAsync(Guid userId, Guid id, RateSupportTicketDto dto, CancellationToken ct = default)
    {
        if (dto.Rating is < 1 or > 5) throw ApiException.BadRequest("التقييم يجب أن يكون من 1 إلى 5"); var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct) ?? throw ApiException.NotFound("تذكرة الدعم غير موجودة");
        if (ticket.Status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed)) throw ApiException.Conflict("يمكن تقييم الخدمة بعد حل التذكرة"); ticket.Rating = dto.Rating; ticket.RatingComment = Clean(dto.Comment, 1000); await db.SaveChangesAsync(ct);
    }

    public async Task<CallbackRequestDto> CallbackAsync(Guid userId, CreateCallbackRequestDto dto, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow; if (dto.PreferredAt < now.AddMinutes(30) || dto.PreferredAt > now.AddDays(30)) throw ApiException.BadRequest("اختر موعدًا خلال الثلاثين يومًا القادمة");
        if (await db.CallbackRequests.AnyAsync(r => r.UserId == userId && (r.Status == CallbackRequestStatus.Requested || r.Status == CallbackRequestStatus.Scheduled), ct)) throw ApiException.Conflict("يوجد طلب مكالمة قائم بالفعل");
        var request = new CallbackRequest { TenantId = TenantId(), UserId = userId, Phone = Required(dto.Phone, 30, "رقم الهاتف مطلوب"), Topic = Required(dto.Topic, 500, "موضوع المكالمة مطلوب"), PreferredAt = dto.PreferredAt }; db.CallbackRequests.Add(request); await db.SaveChangesAsync(ct); return Map(request);
    }

    public Task<List<SupportArticleDto>> ArticlesAsync(string? category, CancellationToken ct = default)
    {
        var q = db.SupportArticles.AsNoTracking(); if (!string.IsNullOrWhiteSpace(category)) q = q.Where(a => a.Category == category); return q.OrderBy(a => a.SortOrder).Select(a => new SupportArticleDto(a.Slug, a.Category, a.QuestionAr, a.AnswerAr)).ToListAsync(ct);
    }

    public async Task<ContentPageDto> PageAsync(string slug, CancellationToken ct = default)
    {
        var p = await db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug, ct) ?? throw ApiException.NotFound("الصفحة غير موجودة"); return new(p.Slug, p.TitleAr, p.BodyAr, p.ContactPhone, p.WhatsAppPhone, p.ContactEmail, p.Address);
    }

    private async Task<SupportAttachment> Store(Guid ticketId, Guid userId, IFormFile file, CancellationToken ct)
    {
        if (file.Length is 0 or > MaxFileBytes || !AllowedTypes.Contains(file.ContentType)) throw ApiException.BadRequest("المرفق يجب أن يكون PDF أو JPG أو PNG وبحد أقصى 10 ميجابايت"); var ext = file.ContentType == "application/pdf" ? ".pdf" : file.ContentType == "image/png" ? ".png" : ".jpg";
        var dir = Path.Combine(env.ContentRootPath, "storage", "tenants", TenantId().ToString(), "support", ticketId.ToString()); Directory.CreateDirectory(dir); var stored = $"{Guid.NewGuid():N}{ext}"; await using (var stream = File.Create(Path.Combine(dir, stored))) await file.CopyToAsync(stream, ct);
        return new SupportAttachment { TenantId = TenantId(), TicketId = ticketId, UploadedByUserId = userId, OriginalName = Path.GetFileName(file.FileName), StoredPath = Path.Combine("storage", "tenants", TenantId().ToString(), "support", ticketId.ToString(), stored).Replace('\\', '/'), ContentType = file.ContentType, SizeBytes = file.Length };
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("هذا الحساب غير مرتبط بشركة");
    private static SupportTicketDetailDto Map(SupportTicket t, IReadOnlyDictionary<Guid, string> names) => new(t.Id, t.Number, t.Type.ToString(), t.Priority.ToString(), t.Status.ToString(), t.Subject, t.Description, t.OrderId, t.CreatedAt, t.ResolvedAt, t.Rating, t.Messages.OrderBy(m => m.CreatedAt).Select(m => new SupportMessageDto(m.Id, m.SenderUserId, names.GetValueOrDefault(m.SenderUserId, m.IsStaff ? "فريق الدعم" : "المستخدم"), m.IsStaff, m.Body, m.CreatedAt, m.ReadAt)).ToList(), t.Attachments.OrderBy(a => a.CreatedAt).Select(a => new SupportAttachmentDto(a.Id, a.OriginalName, a.ContentType, a.SizeBytes, a.CreatedAt)).ToList());
    private static CallbackRequestDto Map(CallbackRequest r) => new(r.Id, r.Phone, r.Topic, r.PreferredAt, r.Status.ToString(), r.CreatedAt);
    private static string Required(string? value, int max, string error) => Clean(value, max) ?? throw ApiException.BadRequest(error);
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var v = value.Trim(); return v.Length <= max ? v : v[..max]; }
}
