using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Engagement;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class EngagementFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TenantProvider _tenant = new();
    private readonly TestEnvironment _env = new(Path.GetTempPath());
    private sealed class TenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class Sms : ISmsSender { public Task SendAsync(string phone, string message, CancellationToken ct = default) => Task.CompletedTask; }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests"; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development"; public string ContentRootPath { get; set; } = root; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    public EngagementFlowTests() { _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open(); _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant); _db.Database.EnsureCreated(); }

    [Fact]
    public async Task Notifications_support_chat_rating_and_callback_are_tenant_safe()
    {
        var tid = Guid.NewGuid(); var uid = Guid.NewGuid(); _tenant.TenantId = tid; var tenant = new Tenant { Id = tid, Name = "Engagement Co", Status = TenantStatus.Active }; var company = new Company { TenantId = tid, LegalName = tenant.Name, Phone = "+201000070001" }; var user = new User { Id = uid, TenantId = tid, FullName = "Client User", Phone = "+201000070002", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1") };
        _db.AddRange(tenant, company, user, new AppNotification { TenantId = tid, UserId = uid, Type = "order.status", Title = "تم شحن الطلب", Body = "الشحنة في الطريق" }); await _db.SaveChangesAsync();
        var notifications = new NotificationService(_db, _tenant); var page = await notifications.ListAsync(uid, 1, 20, false); Assert.Equal(1, page.UnreadCount); await notifications.MarkReadAsync(uid, page.Items[0].Id); Assert.Equal(0, (await notifications.ListAsync(uid, 1, 20, false)).UnreadCount);
        var prefs = await notifications.UpdatePreferencesAsync(uid, new(true, false, true, true, true, false, true, false)); Assert.True(prefs.SmsEnabled); Assert.False(prefs.EmailEnabled);
        var support = new SupportService(_db, _tenant, _env); var ticket = await support.CreateAsync(uid, new("Technical", "High", "تعذر إتمام الدفع", "تظهر رسالة خطأ بعد تأكيد العملية", null), []); Assert.Equal("Open", ticket.Status); Assert.Single(ticket.Messages);
        await support.AddMessageAsync(uid, ticket.Id, new("تمت إعادة المحاولة وما زالت المشكلة قائمة"), []); var entity = await _db.SupportTickets.SingleAsync(t => t.Id == ticket.Id); entity.Status = SupportTicketStatus.Resolved; entity.ResolvedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); await support.RateAsync(uid, ticket.Id, new(5, "حل سريع")); Assert.Equal(5, entity.Rating);
        var callback = await support.CallbackAsync(uid, new(user.Phone, "مراجعة حل المشكلة", DateTime.UtcNow.AddDays(1))); Assert.Equal("Requested", callback.Status);
        _tenant.TenantId = Guid.NewGuid(); Assert.Empty(await notifications.ListAsync(uid, 1, 20, false).ContinueWith(t => t.Result.Items));
    }

    [Fact]
    public async Task Security_settings_change_password_enable_2fa_revoke_sessions_and_schedule_deletion()
    {
        var tid = Guid.NewGuid(); var uid = Guid.NewGuid(); _tenant.TenantId = tid; var tenant = new Tenant { Id = tid, Name = "Secure Co", Status = TenantStatus.Active }; var company = new Company { TenantId = tid, LegalName = tenant.Name, Phone = "+201000080001" }; var user = new User { Id = uid, TenantId = tid, FullName = "Secure User", Phone = "+201000080002", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1") }; var token = new RefreshToken { UserId = uid, TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddDays(3), Device = "Chrome / Windows" }; _db.AddRange(tenant, company, user, token); await _db.SaveChangesAsync();
        var otp = new OtpService(_db, new Sms(), _env); var settings = new SettingsService(_db, _tenant, otp); var appearance = await settings.UpdateAppearanceAsync(uid, new("en", "dark")); Assert.Equal("dark", appearance.Theme);
        var code = await settings.RequestTwoFactorAsync(uid, new("sms")); Assert.NotNull(code); await settings.EnableTwoFactorAsync(uid, new(code!, "sms")); Assert.True(user.TwoFactorEnabled);
        await settings.ChangePasswordAsync(uid, new("OldPass1", "NewSecure2")); Assert.NotNull(token.RevokedAt); Assert.True(BCrypt.Net.BCrypt.Verify("NewSecure2", user.PasswordHash));
        var scheduled = await settings.RequestDeletionAsync(uid, new("NewSecure2", "لم أعد أحتاج الحساب")); Assert.True(scheduled > DateTime.UtcNow.AddDays(29)); await settings.CancelDeletionAsync(uid); Assert.Equal(AccountDeletionStatus.Cancelled, (await _db.AccountDeletionRequests.SingleAsync()).Status);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
