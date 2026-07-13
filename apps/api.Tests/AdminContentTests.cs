using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminContent;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminContentTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminContentService _service;
    private readonly Guid _firstTenant = Guid.NewGuid();
    private readonly Guid _secondTenant = Guid.NewGuid();
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    public AdminContentTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); _service = new AdminContentService(_db); SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var first = new Tenant { Id = _firstTenant, Name = "شركة ألف", Status = TenantStatus.Active };
        var second = new Tenant { Id = _secondTenant, Name = "شركة باء", Status = TenantStatus.Active };
        _db.AddRange(first, second,
            new Company { TenantId = first.Id, Tenant = first, LegalName = first.Name, Phone = "01080000001" },
            new Company { TenantId = second.Id, Tenant = second, LegalName = second.Name, Phone = "01080000002" },
            new User { TenantId = first.Id, FullName = "مستخدم ألف", Phone = "01080000003", IsActive = true },
            new User { TenantId = second.Id, FullName = "مستخدم باء", Phone = "01080000004", IsActive = true });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Content_cycle_manages_home_banners_pages_faq_and_reordering()
    {
        var first = await _service.SaveSectionAsync(null, new("offers", "العروض", 2, true, "{\"limit\":8}"));
        var second = await _service.SaveSectionAsync(null, new("categories", "الأقسام", 1, true, null));
        await _service.ReorderSectionsAsync(new([new(first.Id, 0), new(second.Id, 1)]));

        var banner = await _service.SaveBannerAsync(null, new("خصم الشركات", "لفترة محدودة", "/media/banner.webp", "/offers", _firstTenant, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(2), 0, true));
        Assert.Equal("Scheduled", banner.State); Assert.Equal("شركة ألف", banner.TargetTenantName);

        await _service.SavePageAsync(null, new("returns-policy", "سياسة الاسترجاع", "تفاصيل سياسة الاسترجاع", null, null, null, null, false));
        await _service.SaveFaqAsync(null, new("bulk-orders", "orders", "كيف أطلب كمية كبيرة؟", "اطلب عرض سعر مخصصًا.", 1, true));
        var dashboard = await _service.DashboardAsync();

        Assert.Equal(first.Id, dashboard.Sections[0].Id);
        Assert.Contains(dashboard.Pages, x => x.Slug == "returns-policy" && !x.IsPublished);
        Assert.Contains(dashboard.Faq, x => x.Slug == "bulk-orders");

        var home = await _service.HomeExperienceAsync(_firstTenant);
        Assert.Equal(2, home.Sections.Count);
        Assert.Empty(home.Banners); // scheduled banner must not leak before its start time
    }

    [Fact]
    public async Task Dispatch_cycle_targets_company_and_processes_scheduled_messages_once()
    {
        var immediate = await _service.CreateDispatchAsync(new("AppNotification", "عرض خاص", "خصم لشركة ألف", "/offers", _firstTenant, null, true));
        Assert.Equal("Sent", immediate.Status); Assert.Equal(1, immediate.RecipientCount);
        Assert.Single(await _db.Notifications.Where(x => x.TenantId == _firstTenant).ToListAsync());
        Assert.Empty(await _db.Notifications.Where(x => x.TenantId == _secondTenant).ToListAsync());

        var scheduled = await _service.CreateDispatchAsync(new("InAppMessage", "تحديث الخدمة", "رسالة لكل الشركات", null, null, DateTime.UtcNow.AddMinutes(10), false));
        var entity = await _db.ContentDispatches.SingleAsync(x => x.Id == scheduled.Id); entity.ScheduledAt = DateTime.UtcNow.AddMinutes(-1); await _db.SaveChangesAsync();
        Assert.Equal(1, await _service.ProcessDueAsync());
        Assert.Equal(3, await _db.Notifications.CountAsync());
        Assert.Equal(0, await _service.ProcessDueAsync());
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
