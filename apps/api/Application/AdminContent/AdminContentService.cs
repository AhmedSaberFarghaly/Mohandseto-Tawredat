using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminContent;

public sealed class AdminContentService(AppDbContext db)
{
    public async Task<AdminContentDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var tenants = await db.Tenants.AsNoTracking().OrderBy(x => x.Name).Select(x => new TenantOptionDto(x.Id, x.Name)).ToListAsync(ct);
        var tenantNames = tenants.ToDictionary(x => x.Id, x => x.Name);
        var now = DateTime.UtcNow;
        return new(
            await db.HomeSections.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new HomeSectionDto(x.Id, x.Key, x.NameAr, x.SortOrder, x.IsActive, x.SettingsJson)).ToListAsync(ct),
            (await db.HomeBanners.AsNoTracking().OrderBy(x => x.SortOrder).ThenByDescending(x => x.StartsAt).ToListAsync(ct)).Select(x => Map(x, tenantNames, now)).ToList(),
            await db.ContentPages.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.TitleAr)
                .Select(x => new ContentPageAdminDto(x.Id, x.Slug, x.TitleAr, x.BodyAr, x.ContactPhone, x.WhatsAppPhone, x.ContactEmail, x.Address, x.IsPublished, x.UpdatedAt)).ToListAsync(ct),
            await db.SupportArticles.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder)
                .Select(x => new SupportArticleAdminDto(x.Id, x.Slug, x.Category, x.QuestionAr, x.AnswerAr, x.SortOrder, x.IsPublished)).ToListAsync(ct),
            (await db.ContentDispatches.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct)).Select(x => Map(x, tenantNames)).ToList(),
            tenants);
    }

    public async Task<HomeExperienceDto> HomeExperienceAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sections = await db.HomeSections.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new HomeSectionDto(x.Id, x.Key, x.NameAr, x.SortOrder, x.IsActive, x.SettingsJson)).ToListAsync(ct);
        var bannerEntities = await db.HomeBanners.AsNoTracking()
            .Where(x => x.IsActive && x.StartsAt <= now && (x.EndsAt == null || x.EndsAt > now) && (x.TargetTenantId == null || x.TargetTenantId == tenantId))
            .OrderBy(x => x.SortOrder).ThenByDescending(x => x.StartsAt).ToListAsync(ct);
        var names = await TenantNamesAsync(ct);
        return new(sections, bannerEntities.Select(x => Map(x, names, now)).ToList(), now);
    }

    public async Task<HomeSectionDto> SaveSectionAsync(Guid? id, SaveHomeSectionDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.NameAr)) throw ApiException.BadRequest("مفتاح واسم القسم مطلوبان");
        var key = dto.Key.Trim().ToLowerInvariant();
        if (await db.HomeSections.AnyAsync(x => x.Id != id && x.Key == key, ct)) throw ApiException.Conflict("مفتاح القسم مستخدم بالفعل");
        var entity = id is null ? new HomeSection() : await db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("قسم الصفحة الرئيسية غير موجود");
        entity.Key = key; entity.NameAr = dto.NameAr.Trim(); entity.SortOrder = Math.Max(0, dto.SortOrder); entity.IsActive = dto.IsActive; entity.SettingsJson = Empty(dto.SettingsJson);
        if (id is null) db.HomeSections.Add(entity);
        await db.SaveChangesAsync(ct);
        return new(entity.Id, entity.Key, entity.NameAr, entity.SortOrder, entity.IsActive, entity.SettingsJson);
    }

    public async Task ReorderSectionsAsync(ReorderDto dto, CancellationToken ct = default)
    {
        if (dto.Items.Count == 0 || dto.Items.Select(x => x.Id).Distinct().Count() != dto.Items.Count) throw ApiException.BadRequest("قائمة الترتيب غير صالحة");
        var ids = dto.Items.Select(x => x.Id).ToList(); var entities = await db.HomeSections.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (entities.Count != ids.Count) throw ApiException.BadRequest("أحد أقسام الصفحة الرئيسية غير موجود");
        var order = dto.Items.ToDictionary(x => x.Id, x => x.SortOrder); foreach (var entity in entities) entity.SortOrder = Math.Max(0, order[entity.Id]);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSectionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("قسم الصفحة الرئيسية غير موجود");
        db.HomeSections.Remove(entity); await db.SaveChangesAsync(ct);
    }

    public async Task<HomeBannerDto> SaveBannerAsync(Guid? id, SaveHomeBannerDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.TitleAr) || string.IsNullOrWhiteSpace(dto.ImageUrl)) throw ApiException.BadRequest("عنوان وصورة البنر مطلوبان");
        if (dto.EndsAt is not null && dto.EndsAt <= dto.StartsAt) throw ApiException.BadRequest("موعد انتهاء البنر يجب أن يكون بعد بدايته");
        if (dto.TargetTenantId is not null && !await db.Tenants.AnyAsync(x => x.Id == dto.TargetTenantId, ct)) throw ApiException.BadRequest("الشركة المستهدفة غير موجودة");
        var entity = id is null ? new HomeBanner() : await db.HomeBanners.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("البنر غير موجود");
        entity.TitleAr = dto.TitleAr.Trim(); entity.SubtitleAr = Empty(dto.SubtitleAr); entity.ImageUrl = dto.ImageUrl.Trim(); entity.ActionUrl = Empty(dto.ActionUrl);
        entity.TargetTenantId = dto.TargetTenantId; entity.StartsAt = Utc(dto.StartsAt); entity.EndsAt = dto.EndsAt is null ? null : Utc(dto.EndsAt.Value);
        entity.SortOrder = Math.Max(0, dto.SortOrder); entity.IsActive = dto.IsActive;
        if (id is null) db.HomeBanners.Add(entity); await db.SaveChangesAsync(ct);
        var names = await TenantNamesAsync(ct); return Map(entity, names, DateTime.UtcNow);
    }

    public async Task DeleteBannerAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.HomeBanners.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("البنر غير موجود");
        db.HomeBanners.Remove(entity); await db.SaveChangesAsync(ct);
    }

    public async Task<ContentPageAdminDto> SavePageAsync(Guid? id, SaveContentPageDto dto, CancellationToken ct = default)
    {
        Require(dto.Slug, dto.TitleAr, dto.BodyAr, "الرابط والعنوان والمحتوى مطلوبة"); var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await db.ContentPages.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Id != id && x.Slug == slug, ct)) throw ApiException.Conflict("رابط الصفحة مستخدم بالفعل");
        var entity = id is null ? new ContentPage() : await db.ContentPages.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct) ?? throw ApiException.NotFound("صفحة المحتوى غير موجودة");
        entity.Slug = slug; entity.TitleAr = dto.TitleAr.Trim(); entity.BodyAr = dto.BodyAr.Trim(); entity.ContactPhone = Empty(dto.ContactPhone);
        entity.WhatsAppPhone = Empty(dto.WhatsAppPhone); entity.ContactEmail = Empty(dto.ContactEmail); entity.Address = Empty(dto.Address); entity.IsPublished = dto.IsPublished;
        if (id is null) db.ContentPages.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.Slug, entity.TitleAr, entity.BodyAr, entity.ContactPhone, entity.WhatsAppPhone, entity.ContactEmail, entity.Address, entity.IsPublished, entity.UpdatedAt);
    }

    public async Task<SupportArticleAdminDto> SaveFaqAsync(Guid? id, SaveSupportArticleDto dto, CancellationToken ct = default)
    {
        Require(dto.Slug, dto.Category, dto.QuestionAr, "الرابط والتصنيف والسؤال مطلوبة"); if (string.IsNullOrWhiteSpace(dto.AnswerAr)) throw ApiException.BadRequest("الإجابة مطلوبة");
        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await db.SupportArticles.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Id != id && x.Slug == slug, ct)) throw ApiException.Conflict("رابط السؤال مستخدم بالفعل");
        var entity = id is null ? new SupportArticle() : await db.SupportArticles.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct) ?? throw ApiException.NotFound("السؤال غير موجود");
        entity.Slug = slug; entity.Category = dto.Category.Trim(); entity.QuestionAr = dto.QuestionAr.Trim(); entity.AnswerAr = dto.AnswerAr.Trim(); entity.SortOrder = Math.Max(0, dto.SortOrder); entity.IsPublished = dto.IsPublished;
        if (id is null) db.SupportArticles.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.Slug, entity.Category, entity.QuestionAr, entity.AnswerAr, entity.SortOrder, entity.IsPublished);
    }

    public async Task DeletePageAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.ContentPages.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct) ?? throw ApiException.NotFound("صفحة المحتوى غير موجودة");
        db.ContentPages.Remove(entity); await db.SaveChangesAsync(ct);
    }

    public async Task DeleteFaqAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.SupportArticles.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct) ?? throw ApiException.NotFound("السؤال غير موجود");
        db.SupportArticles.Remove(entity); await db.SaveChangesAsync(ct);
    }

    public async Task<ContentDispatchDto> CreateDispatchAsync(SaveContentDispatchDto dto, CancellationToken ct = default)
    {
        Require(dto.Channel, dto.TitleAr, dto.BodyAr, "القناة والعنوان ونص الرسالة مطلوبة");
        if (!Enum.TryParse<ContentDispatchChannel>(dto.Channel, true, out var channel)) throw ApiException.BadRequest("قناة الإرسال غير صالحة");
        if (dto.TargetTenantId is not null && !await db.Tenants.AnyAsync(x => x.Id == dto.TargetTenantId, ct)) throw ApiException.BadRequest("الشركة المستهدفة غير موجودة");
        DateTime? scheduledAt = dto.ScheduledAt is null ? null : Utc(dto.ScheduledAt.Value); var sendNow = dto.SendNow || scheduledAt <= DateTime.UtcNow;
        var entity = new ContentDispatch { Channel = channel, TitleAr = dto.TitleAr.Trim(), BodyAr = dto.BodyAr.Trim(), ActionUrl = Empty(dto.ActionUrl), TargetTenantId = dto.TargetTenantId, ScheduledAt = scheduledAt, Status = sendNow ? ContentDispatchStatus.Draft : scheduledAt is null ? ContentDispatchStatus.Draft : ContentDispatchStatus.Scheduled };
        db.ContentDispatches.Add(entity); await db.SaveChangesAsync(ct); if (sendNow) await DeliverAsync(entity, ct);
        return Map(entity, await TenantNamesAsync(ct));
    }

    public async Task<ContentDispatchDto> SendAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.ContentDispatches.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الرسالة غير موجودة");
        await DeliverAsync(entity, ct); return Map(entity, await TenantNamesAsync(ct));
    }

    public async Task<int> ProcessDueAsync(CancellationToken ct = default)
    {
        var due = await db.ContentDispatches.Where(x => x.Status == ContentDispatchStatus.Scheduled && x.ScheduledAt <= DateTime.UtcNow).ToListAsync(ct);
        foreach (var entity in due) await DeliverAsync(entity, ct); return due.Count;
    }

    private async Task DeliverAsync(ContentDispatch entity, CancellationToken ct)
    {
        if (entity.Status == ContentDispatchStatus.Sent) throw ApiException.Conflict("تم إرسال هذه الرسالة بالفعل");
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted && x.IsActive && !x.IsPlatformStaff && x.TenantId != null && (entity.TargetTenantId == null || x.TenantId == entity.TargetTenantId)).Select(x => new { x.Id, TenantId = x.TenantId!.Value }).ToListAsync(ct);
        db.Notifications.AddRange(users.Select(x => new AppNotification { TenantId = x.TenantId, UserId = x.Id, Type = entity.Channel == ContentDispatchChannel.InAppMessage ? "in_app_message" : "admin_broadcast", Title = entity.TitleAr, Body = entity.BodyAr, EntityType = "content_dispatch", EntityId = entity.Id }));
        entity.RecipientCount = users.Count; entity.SentAt = DateTime.UtcNow; entity.Status = ContentDispatchStatus.Sent; await db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> TenantNamesAsync(CancellationToken ct) => await db.Tenants.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    private static HomeBannerDto Map(HomeBanner x, IReadOnlyDictionary<Guid, string> names, DateTime now) => new(x.Id, x.TitleAr, x.SubtitleAr, x.ImageUrl, x.ActionUrl, x.TargetTenantId, x.TargetTenantId is { } id && names.TryGetValue(id, out var name) ? name : null, x.StartsAt, x.EndsAt, x.SortOrder, x.IsActive, !x.IsActive ? "Inactive" : x.StartsAt > now ? "Scheduled" : x.EndsAt <= now ? "Expired" : "Active");
    private static ContentDispatchDto Map(ContentDispatch x, IReadOnlyDictionary<Guid, string> names) => new(x.Id, x.Channel.ToString(), x.TitleAr, x.BodyAr, x.ActionUrl, x.TargetTenantId, x.TargetTenantId is { } id && names.TryGetValue(id, out var name) ? name : null, x.ScheduledAt, x.SentAt, x.Status.ToString(), x.RecipientCount, x.CreatedAt);
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Require(string a, string b, string c, string message) { if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) || string.IsNullOrWhiteSpace(c)) throw ApiException.BadRequest(message); }
}
