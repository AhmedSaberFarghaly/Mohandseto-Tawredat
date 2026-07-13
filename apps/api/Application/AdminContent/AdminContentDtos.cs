namespace Mohandseto.Api.Application.AdminContent;

public sealed record HomeSectionDto(Guid Id, string Key, string NameAr, int SortOrder, bool IsActive, string? SettingsJson);
public sealed record SaveHomeSectionDto(string Key, string NameAr, int SortOrder, bool IsActive, string? SettingsJson);
public sealed record ReorderItemDto(Guid Id, int SortOrder);
public sealed record ReorderDto(IReadOnlyList<ReorderItemDto> Items);

public sealed record HomeBannerDto(Guid Id, string TitleAr, string? SubtitleAr, string ImageUrl, string? ActionUrl,
    Guid? TargetTenantId, string? TargetTenantName, DateTime StartsAt, DateTime? EndsAt, int SortOrder, bool IsActive, string State);
public sealed record SaveHomeBannerDto(string TitleAr, string? SubtitleAr, string ImageUrl, string? ActionUrl,
    Guid? TargetTenantId, DateTime StartsAt, DateTime? EndsAt, int SortOrder, bool IsActive);

public sealed record ContentPageAdminDto(Guid Id, string Slug, string TitleAr, string BodyAr, string? ContactPhone,
    string? WhatsAppPhone, string? ContactEmail, string? Address, bool IsPublished, DateTime? UpdatedAt);
public sealed record SaveContentPageDto(string Slug, string TitleAr, string BodyAr, string? ContactPhone,
    string? WhatsAppPhone, string? ContactEmail, string? Address, bool IsPublished);

public sealed record SupportArticleAdminDto(Guid Id, string Slug, string Category, string QuestionAr, string AnswerAr,
    int SortOrder, bool IsPublished);
public sealed record SaveSupportArticleDto(string Slug, string Category, string QuestionAr, string AnswerAr,
    int SortOrder, bool IsPublished);

public sealed record ContentDispatchDto(Guid Id, string Channel, string TitleAr, string BodyAr, string? ActionUrl,
    Guid? TargetTenantId, string? TargetTenantName, DateTime? ScheduledAt, DateTime? SentAt, string Status,
    int RecipientCount, DateTime CreatedAt);
public sealed record SaveContentDispatchDto(string Channel, string TitleAr, string BodyAr, string? ActionUrl,
    Guid? TargetTenantId, DateTime? ScheduledAt, bool SendNow);
public sealed record TenantOptionDto(Guid Id, string Name);
public sealed record AdminContentDashboardDto(IReadOnlyList<HomeSectionDto> Sections, IReadOnlyList<HomeBannerDto> Banners,
    IReadOnlyList<ContentPageAdminDto> Pages, IReadOnlyList<SupportArticleAdminDto> Faq,
    IReadOnlyList<ContentDispatchDto> Dispatches, IReadOnlyList<TenantOptionDto> Tenants);
public sealed record HomeExperienceDto(IReadOnlyList<HomeSectionDto> Sections, IReadOnlyList<HomeBannerDto> Banners, DateTime GeneratedAt);
