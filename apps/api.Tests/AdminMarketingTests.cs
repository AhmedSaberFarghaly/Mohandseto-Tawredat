using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminMarketing;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminMarketingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminMarketingService _service;
    private readonly Guid _foodTenant = Guid.NewGuid();
    private readonly Guid _healthTenant = Guid.NewGuid();
    private Guid _foodUser;

    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class AcceptedSender : IMarketingChannelSender
    {
        public Task<MarketingSendResult> SendAsync(MarketingCampaignChannel channel, string destination, string title,
            string body, string? actionUrl, CancellationToken ct) => Task.FromResult(new MarketingSendResult(true, $"test-{channel}-{destination}", null));
    }

    public AdminMarketingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); _service = new AdminMarketingService(_db, new AcceptedSender());
        SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var food = new Tenant { Id = _foodTenant, Name = "شركة الأغذية", Status = TenantStatus.Active };
        var health = new Tenant { Id = _healthTenant, Name = "شركة الرعاية", Status = TenantStatus.Active };
        _foodUser = Guid.NewGuid();
        _db.AddRange(food, health,
            new Company { TenantId = food.Id, Tenant = food, LegalName = food.Name, Phone = "01000000001", Sector = "مقاولات" },
            new Company { TenantId = health.Id, Tenant = health, LegalName = health.Name, Phone = "01000000002", Sector = "رعاية صحية" },
            new User { Id = _foodUser, TenantId = food.Id, FullName = "مسؤول المشتريات", Phone = "01000000003", Email = "food@example.com", IsActive = true },
            new User { TenantId = health.Id, FullName = "مدير الشركة", Phone = "01000000004", Email = "health@example.com", IsActive = true });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Campaign_targets_selected_companies_delivers_and_tracks_events()
    {
        var campaign = await _service.CreateCampaignAsync(new("عرض يوليو", "Push", "SelectedCompanies", null, null,
            [_foodTenant], "خصم خاص", "اطبع هويتك بخصم حصري", "/offers", null, null, "Immediate", null, 250, true));

        Assert.Equal("Sent", campaign.Status);
        Assert.Equal(1, campaign.Recipients);
        Assert.Single(await _db.MarketingDeliveries.ToListAsync());
        Assert.Single(await _db.Notifications.Where(x => x.TenantId == _foodTenant).ToListAsync());
        Assert.Empty(await _db.Notifications.Where(x => x.TenantId == _healthTenant).ToListAsync());

        await _service.TrackAsync(campaign.Id, _foodUser, new("click", null));
        var detail = await _service.GetCampaignAsync(campaign.Id);
        Assert.Equal(1, detail.Opened); Assert.Equal(1, detail.Clicked); Assert.Equal(100, detail.OpenRate);
    }

    [Fact]
    public async Task Scheduled_campaign_is_processed_once_and_sector_coupon_fans_out_safely()
    {
        var scheduled = await _service.CreateCampaignAsync(new("رسالة عقود", "WhatsApp", "AllCompanies", null, null,
            [], "تجديد العقود", "يرجى مراجعة موعد التجديد", null, null, null, "Scheduled", DateTime.UtcNow.AddMinutes(5), 0, false));
        var entity = await _db.MarketingCampaigns.SingleAsync(x => x.Id == scheduled.Id);
        entity.ScheduledAt = DateTime.UtcNow.AddMinutes(-1); await _db.SaveChangesAsync();
        Assert.Equal(1, await _service.ProcessDueAsync());
        Assert.Equal(0, await _service.ProcessDueAsync());
        Assert.Equal(2, await _db.MarketingDeliveries.CountAsync());

        var coupon = await _service.SaveCouponAsync(new(null, "BUILD15", "خصم المقاولات", "Percentage", 15, 1000, 2000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 100, true, false, true, false, [], "Sector", "مقاولات", [], true));
        Assert.Equal(1, coupon.CompanyCount);
        var stored = await _db.Coupons.IgnoreQueryFilters().SingleAsync(x => x.CampaignGroupId == coupon.GroupId);
        Assert.Equal(_foodTenant, stored.TenantId); Assert.True(stored.OncePerCompany); Assert.False(stored.CanCombine);

        var updated = await _service.SaveCouponAsync(new(coupon.GroupId, "BUILD15", "خصم المقاولات المحدث", "Percentage", 20, 1200, 2500,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(45), 120, true, false, true, false, [], "Sector", "مقاولات", [], true));
        Assert.Equal(20, updated.DiscountValue);
        Assert.Single(await _db.Coupons.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.CampaignGroupId == coupon.GroupId).ToListAsync());
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
