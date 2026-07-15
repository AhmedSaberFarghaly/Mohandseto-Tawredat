namespace Mohandseto.Api.Application.AdminMarketing;

public sealed record MarketingTenantOptionDto(Guid Id, string Name, string? Sector, DateTime? LastOrderAt);
public sealed record MarketingCategoryOptionDto(Guid Id, string Name);
public sealed record MarketingCampaignDto(Guid Id, string Number, string Name, string Channel, string AudienceType,
    string? Sector, int? BehaviorDays, string Title, string Body, string? ActionUrl, string? ImageUrl,
    string? CouponCode, string ScheduleType, DateTime? ScheduledAt, DateTime? SentAt, string Status, decimal Cost,
    int Recipients, int Delivered, int Opened, int Clicked, int Conversions, decimal Revenue, decimal OpenRate,
    decimal ClickRate, decimal ConversionRate, DateTime CreatedAt, IReadOnlyList<Guid> TenantIds);

public sealed record SaveMarketingCampaignDto(string Name, string Channel, string AudienceType, string? Sector,
    int? BehaviorDays, IReadOnlyList<Guid> TenantIds, string Title, string Body, string? ActionUrl, string? ImageUrl,
    string? CouponCode, string ScheduleType, DateTime? ScheduledAt, decimal Cost, bool SendNow);

public sealed record MarketingCouponDto(Guid GroupId, string Code, string NameAr, string DiscountType,
    decimal DiscountValue, decimal MinimumSubtotal, decimal? MaximumDiscount, DateTime? StartsAt, DateTime? ExpiresAt,
    int? UsageLimit, int UsedCount, bool OncePerCompany, bool NewCustomersOnly, bool ExcludeDiscountedProducts,
    bool CanCombine, string? ApplicableCategoryIds, bool IsActive, int CompanyCount, IReadOnlyList<Guid> TenantIds);

public sealed record SaveMarketingCouponDto(Guid? GroupId, string Code, string NameAr, string DiscountType,
    decimal DiscountValue, decimal MinimumSubtotal, decimal? MaximumDiscount, DateTime? StartsAt, DateTime? ExpiresAt,
    int? UsageLimit, bool OncePerCompany, bool NewCustomersOnly, bool ExcludeDiscountedProducts, bool CanCombine,
    IReadOnlyList<Guid> CategoryIds, string AudienceType, string? Sector, IReadOnlyList<Guid> TenantIds, bool IsActive);

public sealed record ChannelRateDto(string Channel, int Delivered, int Opened, decimal OpenRate);
public sealed record BestTimeDto(int Hour, int Opened, decimal OpenRate);
public sealed record MarketingReportsDto(IReadOnlyList<ChannelRateDto> ChannelRates, IReadOnlyList<BestTimeDto> BestTimes,
    int Delivered, int Opened, int Clicked, int Conversions, decimal Revenue, decimal Cost, decimal Roi,
    decimal OpenRate, decimal ConversionRate);

public sealed record AdminMarketingDashboardDto(IReadOnlyList<MarketingCampaignDto> Campaigns,
    IReadOnlyList<MarketingCouponDto> Coupons, IReadOnlyList<MarketingTenantOptionDto> Tenants,
    IReadOnlyList<MarketingCategoryOptionDto> Categories, IReadOnlyList<string> Sectors, MarketingReportsDto Reports);

public sealed record TrackMarketingEventDto(string EventType, decimal? Revenue);
