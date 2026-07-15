using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminMarketing;

public sealed class AdminMarketingService(AppDbContext db, IMarketingChannelSender sender)
{
    private sealed record CouponOrderMetric(Guid TenantId, string? CouponCode, DateTime CreatedAt, decimal Total);

    public async Task<AdminMarketingDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var companies = await db.Companies.AsNoTracking().Include(x => x.Tenant)
            .Where(x => x.Tenant.Status == TenantStatus.Active).OrderBy(x => x.LegalName).ToListAsync(ct);
        var lastOrders = await db.Orders.AsNoTracking().GroupBy(x => x.TenantId)
            .Select(x => new { TenantId = x.Key, Last = (DateTime?)x.Max(o => o.CreatedAt) }).ToDictionaryAsync(x => x.TenantId, x => x.Last, ct);
        var tenants = companies.Select(x => new MarketingTenantOptionDto(x.TenantId, x.LegalName, x.Sector,
            lastOrders.GetValueOrDefault(x.TenantId))).ToList();

        var campaigns = await db.MarketingCampaigns.AsNoTracking().AsSplitQuery().Include(x => x.TargetTenants)
            .Include(x => x.Deliveries).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct);
        var couponOrders = await db.Orders.AsNoTracking().Where(x => x.CouponCode != null && x.Status != OrderStatus.Cancelled)
            .Select(x => new CouponOrderMetric(x.TenantId, x.CouponCode, x.CreatedAt, x.Total)).ToListAsync(ct);
        var mapped = campaigns.Select(x => MapCampaign(x, couponOrders)).ToList();
        var coupons = await CouponsAsync(ct);
        var reports = BuildReports(campaigns, mapped);
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.NameAr)
            .Select(x => new MarketingCategoryOptionDto(x.Id, x.NameAr)).ToListAsync(ct);
        return new(mapped, coupons, tenants, categories,
            companies.Select(x => x.Sector).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().Order().ToList(), reports);
    }

    public async Task<MarketingCampaignDto> CreateCampaignAsync(SaveMarketingCampaignDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
            throw ApiException.BadRequest("اسم الحملة والعنوان والمحتوى مطلوبة");
        var channel = Parse<MarketingCampaignChannel>(dto.Channel, "قناة الحملة غير صالحة");
        var audience = Parse<MarketingAudienceType>(dto.AudienceType, "نوع الجمهور غير صالح");
        var schedule = Parse<MarketingScheduleType>(dto.ScheduleType, "نوع الجدولة غير صالح");
        var tenantIds = await ResolveTenantIdsAsync(audience, dto.TenantIds, dto.Sector, dto.BehaviorDays, ct);
        if (tenantIds.Count == 0) throw ApiException.BadRequest("لا توجد شركات مطابقة للجمهور المحدد");
        if (schedule == MarketingScheduleType.Scheduled && dto.ScheduledAt is null)
            throw ApiException.BadRequest("موعد الإرسال مطلوب للحملة المجدولة");
        DateTime? scheduledAt = schedule == MarketingScheduleType.Optimal
            ? NextOptimalAt(DateTime.UtcNow)
            : dto.ScheduledAt is null ? null : Utc(dto.ScheduledAt.Value);
        if (scheduledAt <= DateTime.UtcNow && !dto.SendNow) throw ApiException.BadRequest("موعد الإرسال يجب أن يكون في المستقبل");

        var entity = new MarketingCampaign
        {
            Number = $"MKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Name = dto.Name.Trim(), Channel = channel, AudienceType = audience, Sector = Clean(dto.Sector),
            BehaviorDays = audience == MarketingAudienceType.Behavior ? Math.Clamp(dto.BehaviorDays ?? 30, 1, 365) : null,
            Title = dto.Title.Trim(), Body = dto.Body.Trim(), ActionUrl = Clean(dto.ActionUrl), ImageUrl = Clean(dto.ImageUrl),
            CouponCode = Clean(dto.CouponCode)?.ToUpperInvariant(), ScheduleType = schedule, ScheduledAt = scheduledAt,
            Cost = Math.Max(0, dto.Cost), Status = dto.SendNow || schedule == MarketingScheduleType.Immediate
                ? MarketingCampaignStatus.Draft : MarketingCampaignStatus.Scheduled,
        };
        entity.TargetTenants = tenantIds.Select(x => new MarketingCampaignTenant { TenantId = x }).ToList();
        db.MarketingCampaigns.Add(entity); await db.SaveChangesAsync(ct);
        if (dto.SendNow || schedule == MarketingScheduleType.Immediate) await DeliverAsync(entity.Id, ct);
        return await GetCampaignAsync(entity.Id, ct);
    }

    public async Task<MarketingCampaignDto> GetCampaignAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await db.MarketingCampaigns.AsNoTracking().AsSplitQuery().Include(x => x.TargetTenants).Include(x => x.Deliveries)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الحملة غير موجودة");
        var orders = string.IsNullOrWhiteSpace(campaign.CouponCode) ? [] : await db.Orders.AsNoTracking()
            .Where(x => x.CouponCode == campaign.CouponCode && x.Status != OrderStatus.Cancelled)
            .Select(x => new CouponOrderMetric(x.TenantId, x.CouponCode, x.CreatedAt, x.Total)).ToListAsync(ct);
        return MapCampaign(campaign, orders);
    }

    public async Task<MarketingCampaignDto> SendAsync(Guid id, CancellationToken ct = default)
    {
        await DeliverAsync(id, ct); return await GetCampaignAsync(id, ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await db.MarketingCampaigns.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("الحملة غير موجودة");
        if (campaign.Status is MarketingCampaignStatus.Sent or MarketingCampaignStatus.Processing)
            throw ApiException.Conflict("لا يمكن إلغاء حملة بدأ إرسالها");
        campaign.Status = MarketingCampaignStatus.Cancelled; await db.SaveChangesAsync(ct);
    }

    public async Task<int> ProcessDueAsync(CancellationToken ct = default)
    {
        var ids = await db.MarketingCampaigns.Where(x => x.Status == MarketingCampaignStatus.Scheduled && x.ScheduledAt <= DateTime.UtcNow)
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids) await DeliverAsync(id, ct);
        return ids.Count;
    }

    public async Task TrackAsync(Guid campaignId, Guid userId, TrackMarketingEventDto dto, CancellationToken ct = default)
    {
        var delivery = await db.MarketingDeliveries.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.UserId == userId, ct)
            ?? throw ApiException.NotFound("سجل استلام الحملة غير موجود");
        var now = DateTime.UtcNow;
        switch (dto.EventType.Trim().ToLowerInvariant())
        {
            case "open": delivery.OpenedAt ??= now; break;
            case "click": delivery.OpenedAt ??= now; delivery.ClickedAt ??= now; break;
            case "conversion": delivery.OpenedAt ??= now; delivery.ConvertedAt ??= now; delivery.ConversionRevenue += Math.Max(0, dto.Revenue ?? 0); break;
            default: throw ApiException.BadRequest("نوع حدث الحملة غير صالح");
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<MarketingCouponDto> SaveCouponAsync(SaveMarketingCouponDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NameAr))
            throw ApiException.BadRequest("كود واسم الكوبون مطلوبان");
        var type = Parse<CouponDiscountType>(dto.DiscountType, "نوع الخصم غير صالح");
        if (dto.DiscountValue <= 0 || type == CouponDiscountType.Percentage && dto.DiscountValue > 100)
            throw ApiException.BadRequest("قيمة الخصم غير صالحة");
        if (dto.ExpiresAt is not null && dto.StartsAt is not null && dto.ExpiresAt <= dto.StartsAt)
            throw ApiException.BadRequest("تاريخ انتهاء الكوبون يجب أن يكون بعد بدايته");
        var audience = Parse<MarketingAudienceType>(dto.AudienceType, "نوع جمهور الكوبون غير صالح");
        var tenantIds = await ResolveTenantIdsAsync(audience, dto.TenantIds, dto.Sector, null, ct);
        if (tenantIds.Count == 0) throw ApiException.BadRequest("لا توجد شركات مطابقة لجمهور الكوبون");
        var code = dto.Code.Trim().ToUpperInvariant(); var groupId = dto.GroupId ?? Guid.NewGuid();
        var existing = await db.Coupons.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.CampaignGroupId == groupId).ToListAsync(ct);
        var conflicts = await db.Coupons.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.CampaignGroupId != groupId &&
            tenantIds.Contains(x.TenantId) && x.Code == code, ct);
        if (conflicts) throw ApiException.Conflict("الكود مستخدم بالفعل لدى إحدى الشركات المستهدفة");
        var categories = dto.CategoryIds.Count == 0 ? null : JsonSerializer.Serialize(dto.CategoryIds.Distinct());
        var coupons = new List<Coupon>();
        foreach (var tenantId in tenantIds)
        {
            var coupon = existing.FirstOrDefault(x => x.TenantId == tenantId) ?? new Coupon { TenantId = tenantId, CampaignGroupId = groupId };
            coupon.Code = code; coupon.NameAr = dto.NameAr.Trim(); coupon.DiscountType = type; coupon.DiscountValue = dto.DiscountValue;
            coupon.MinimumSubtotal = Math.Max(0, dto.MinimumSubtotal);
            coupon.MaximumDiscount = dto.MaximumDiscount is null ? null : Math.Max(0, dto.MaximumDiscount.Value);
            coupon.StartsAt = dto.StartsAt is null ? null : Utc(dto.StartsAt.Value);
            coupon.ExpiresAt = dto.ExpiresAt is null ? null : Utc(dto.ExpiresAt.Value);
            coupon.UsageLimit = dto.UsageLimit is null ? null : Math.Max(1, dto.UsageLimit.Value);
            coupon.OncePerCompany = dto.OncePerCompany; coupon.NewCustomersOnly = dto.NewCustomersOnly;
            coupon.ExcludeDiscountedProducts = dto.ExcludeDiscountedProducts; coupon.CanCombine = dto.CanCombine;
            coupon.ApplicableCategoryIds = categories; coupon.IsActive = dto.IsActive;
            if (!existing.Contains(coupon)) db.Coupons.Add(coupon);
            coupons.Add(coupon);
        }
        var removed = existing.Where(x => !tenantIds.Contains(x.TenantId)).ToList();
        if (removed.Count > 0) db.Coupons.RemoveRange(removed);
        await db.SaveChangesAsync(ct);
        return (await CouponsAsync(ct)).Single(x => x.GroupId == groupId);
    }

    public async Task SetCouponStateAsync(Guid groupId, bool isActive, CancellationToken ct = default)
    {
        var coupons = await db.Coupons.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.CampaignGroupId == groupId).ToListAsync(ct);
        if (coupons.Count == 0) throw ApiException.NotFound("الكوبون غير موجود");
        foreach (var coupon in coupons) coupon.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    private async Task DeliverAsync(Guid id, CancellationToken ct)
    {
        var campaign = await db.MarketingCampaigns.Include(x => x.TargetTenants).Include(x => x.Deliveries)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الحملة غير موجودة");
        if (campaign.Status == MarketingCampaignStatus.Sent) throw ApiException.Conflict("تم إرسال الحملة بالفعل");
        if (campaign.Status == MarketingCampaignStatus.Cancelled) throw ApiException.Conflict("الحملة ملغاة");
        campaign.Status = MarketingCampaignStatus.Processing; await db.SaveChangesAsync(ct);
        var targets = campaign.TargetTenants.Select(x => x.TenantId).ToList();
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive && x.TenantId != null && targets.Contains(x.TenantId.Value)).ToListAsync(ct);
        var preferences = await db.NotificationPreferences.AsNoTracking().Where(x => targets.Contains(x.TenantId)).ToDictionaryAsync(x => x.UserId, ct);
        users = users.Where(user => !preferences.TryGetValue(user.Id, out var pref) || pref.PromotionsEnabled &&
            (campaign.Channel != MarketingCampaignChannel.Push || pref.PushEnabled) &&
            (campaign.Channel != MarketingCampaignChannel.Email || pref.EmailEnabled)).ToList();
        var recipientCount = 0;
        foreach (var user in users)
        {
            var tenantId = user.TenantId!.Value;
            var destination = campaign.Channel switch
            {
                MarketingCampaignChannel.Email => user.Email,
                MarketingCampaignChannel.WhatsApp => user.Phone,
                _ => user.Id.ToString(),
            };
            if (string.IsNullOrWhiteSpace(destination)) continue;
            recipientCount++;
            var delivery = campaign.Deliveries.FirstOrDefault(x => x.UserId == user.Id);
            var isNew = delivery is null;
            delivery ??= new MarketingDelivery
            {
                CampaignId = campaign.Id, TenantId = tenantId, UserId = user.Id, Destination = destination,
            };
            if (isNew) db.MarketingDeliveries.Add(delivery);
            if (campaign.Channel is MarketingCampaignChannel.Push or MarketingCampaignChannel.InApp)
            {
                db.Notifications.Add(new AppNotification { TenantId = tenantId, UserId = user.Id,
                    Type = campaign.Channel == MarketingCampaignChannel.InApp ? "marketing_in_app" : "marketing_push",
                    Title = campaign.Title, Body = campaign.Body, EntityType = "marketing_campaign", EntityId = campaign.Id });
                delivery.Status = MarketingDeliveryStatus.Delivered; delivery.DeliveredAt = DateTime.UtcNow;
                delivery.ProviderReference = $"app-{Guid.NewGuid():N}";
            }
            else
            {
                var result = await sender.SendAsync(campaign.Channel, destination, campaign.Title, campaign.Body, campaign.ActionUrl, ct);
                delivery.Status = result.Accepted ? MarketingDeliveryStatus.Delivered : MarketingDeliveryStatus.Failed;
                delivery.DeliveredAt = result.Accepted ? DateTime.UtcNow : null; delivery.ProviderReference = result.ProviderReference;
                delivery.FailureReason = result.Error;
            }
        }
        campaign.RecipientCount = recipientCount; campaign.SentAt = DateTime.UtcNow; campaign.Status = MarketingCampaignStatus.Sent;
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<Guid>> ResolveTenantIdsAsync(MarketingAudienceType audience, IReadOnlyList<Guid> selected,
        string? sector, int? behaviorDays, CancellationToken ct)
    {
        var active = db.Companies.AsNoTracking().Where(x => x.Tenant.Status == TenantStatus.Active);
        return audience switch
        {
            MarketingAudienceType.AllCompanies => await active.Select(x => x.TenantId).ToListAsync(ct),
            MarketingAudienceType.SelectedCompanies => await active.Where(x => selected.Contains(x.TenantId)).Select(x => x.TenantId).ToListAsync(ct),
            MarketingAudienceType.Sector when !string.IsNullOrWhiteSpace(sector) => await active.Where(x => x.Sector == sector.Trim()).Select(x => x.TenantId).ToListAsync(ct),
            MarketingAudienceType.Behavior => await db.Orders.AsNoTracking().Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-Math.Clamp(behaviorDays ?? 30, 1, 365)))
                .Select(x => x.TenantId).Distinct().ToListAsync(ct),
            MarketingAudienceType.Sector => throw ApiException.BadRequest("القطاع مطلوب"),
            _ => [],
        };
    }

    private async Task<List<MarketingCouponDto>> CouponsAsync(CancellationToken ct)
    {
        var entities = await db.Coupons.IgnoreQueryFilters().AsNoTracking()
            .Where(x => !x.IsDeleted && x.CampaignGroupId != null).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return entities.GroupBy(x => x.CampaignGroupId!.Value).Select(group =>
        {
            var x = group.First();
            return new MarketingCouponDto(group.Key, x.Code, x.NameAr, x.DiscountType.ToString(), x.DiscountValue,
                x.MinimumSubtotal, x.MaximumDiscount, x.StartsAt, x.ExpiresAt, x.UsageLimit, group.Sum(c => c.UsedCount),
                x.OncePerCompany, x.NewCustomersOnly, x.ExcludeDiscountedProducts, x.CanCombine, x.ApplicableCategoryIds,
                group.Any(c => c.IsActive), group.Count(), group.Select(c => c.TenantId).ToList());
        }).ToList();
    }

    private static MarketingCampaignDto MapCampaign(MarketingCampaign x, IEnumerable<CouponOrderMetric> orders)
    {
        var delivered = x.Deliveries.Count(d => d.Status == MarketingDeliveryStatus.Delivered);
        var opened = x.Deliveries.Count(d => d.OpenedAt != null); var clicked = x.Deliveries.Count(d => d.ClickedAt != null);
        var matchingOrders = string.IsNullOrWhiteSpace(x.CouponCode) ? [] : orders.Where(o => o.CouponCode == x.CouponCode &&
            o.CreatedAt >= (x.SentAt ?? x.CreatedAt) && x.TargetTenants.Any(t => t.TenantId == o.TenantId)).ToList();
        var trackedConversions = x.Deliveries.Count(d => d.ConvertedAt != null); var conversions = Math.Max(trackedConversions, matchingOrders.Count);
        var revenue = Math.Max(x.Deliveries.Sum(d => d.ConversionRevenue), matchingOrders.Sum(o => o.Total));
        return new(x.Id, x.Number, x.Name, x.Channel.ToString(), x.AudienceType.ToString(), x.Sector, x.BehaviorDays,
            x.Title, x.Body, x.ActionUrl, x.ImageUrl, x.CouponCode, x.ScheduleType.ToString(), x.ScheduledAt, x.SentAt,
            x.Status.ToString(), x.Cost, x.RecipientCount, delivered, opened, clicked, conversions, revenue,
            Rate(opened, delivered), Rate(clicked, delivered), Rate(conversions, delivered), x.CreatedAt,
            x.TargetTenants.Select(t => t.TenantId).ToList());
    }

    private static MarketingReportsDto BuildReports(IReadOnlyList<MarketingCampaign> campaigns, IReadOnlyList<MarketingCampaignDto> mapped)
    {
        var rates = campaigns.GroupBy(x => x.Channel).Select(group =>
        {
            var delivered = group.Sum(x => x.Deliveries.Count(d => d.Status == MarketingDeliveryStatus.Delivered));
            var opened = group.Sum(x => x.Deliveries.Count(d => d.OpenedAt != null));
            return new ChannelRateDto(group.Key.ToString(), delivered, opened, Rate(opened, delivered));
        }).ToList();
        var best = campaigns.SelectMany(x => x.Deliveries).Where(x => x.DeliveredAt != null).GroupBy(x => x.DeliveredAt!.Value.Hour)
            .Select(g => new BestTimeDto(g.Key, g.Count(x => x.OpenedAt != null), Rate(g.Count(x => x.OpenedAt != null), g.Count())))
            .OrderByDescending(x => x.OpenRate).Take(5).ToList();
        var deliveredAll = mapped.Sum(x => x.Delivered); var openedAll = mapped.Sum(x => x.Opened); var clickedAll = mapped.Sum(x => x.Clicked);
        var conversions = mapped.Sum(x => x.Conversions); var revenue = mapped.Sum(x => x.Revenue); var cost = mapped.Sum(x => x.Cost);
        return new(rates, best, deliveredAll, openedAll, clickedAll, conversions, revenue, cost,
            cost == 0 ? 0 : decimal.Round((revenue - cost) / cost, 2), Rate(openedAll, deliveredAll), Rate(conversions, deliveredAll));
    }

    private static decimal Rate(int value, int total) => total == 0 ? 0 : decimal.Round((decimal)value * 100 / total, 1);
    private static T Parse<T>(string value, string message) where T : struct => Enum.TryParse<T>(value, true, out var parsed) ? parsed : throw ApiException.BadRequest(message);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static DateTime NextOptimalAt(DateTime now)
    {
        var candidate = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0, DateTimeKind.Utc);
        if (candidate <= now) candidate = candidate.AddDays(1);
        while (candidate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) candidate = candidate.AddDays(1);
        return candidate;
    }
}
