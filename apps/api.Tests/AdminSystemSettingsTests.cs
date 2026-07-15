using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminSystemSettingsTests : IDisposable
{
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Mohandseto.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mohandseto-settings-{Guid.NewGuid():N}");
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminSystemSettingsService _service;
    private readonly Guid _actor = Guid.NewGuid();

    public AdminSystemSettingsTests()
    {
        Directory.CreateDirectory(_root);
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated();
        _service = new AdminSystemSettingsService(_db, DataProtectionProvider.Create(Path.Combine(_root, "keys")), new TestEnvironment(_root));
    }

    [Fact]
    public async Task Dashboard_covers_all_dynamic_sections_and_defaults()
    {
        var dashboard = await _service.DashboardAsync();
        Assert.Equal(29, dashboard.Sections.Count);
        Assert.Contains(dashboard.Sections, x => x.Code == "general" && x.Values["platformName"].Length > 0);
        Assert.Contains(dashboard.Sections, x => x.Code == "security" && x.Values["lockAttempts"] == "5");
        Assert.Empty(dashboard.ApiKeys);
    }

    [Fact]
    public async Task Sensitive_settings_are_encrypted_and_runtime_app_config_is_updated()
    {
        var email = (await _service.DashboardAsync()).Sections.Single(x => x.Code == "email");
        var values = email.Values.ToDictionary(); values["apiKey"] = "provider-secret-value";
        await _service.SaveSectionAsync(_actor, "127.0.0.1", "email", new(values));
        var stored = await _db.SystemSettings.SingleAsync(x => x.Section == "email" && x.Key == "apiKey");
        Assert.True(stored.IsProtected); Assert.DoesNotContain("provider-secret-value", stored.Value);
        Assert.Equal("provider-secret-value", await _service.ValueAsync("email", "apiKey", ""));
        Assert.Single(await _db.IntegrationOperationLogs.ToListAsync());

        var maintenance = (await _service.DashboardAsync()).Sections.Single(x => x.Code == "maintenance").Values.ToDictionary();
        maintenance["enabled"] = "true"; maintenance["message"] = "صيانة مجدولة";
        await _service.SaveSectionAsync(_actor, null, "maintenance", new(maintenance));
        var app = await _db.MobileAppConfigs.SingleAsync(x => x.Platform == "all");
        Assert.True(app.MaintenanceEnabled); Assert.Equal("صيانة مجدولة", app.MessageAr);
    }

    [Fact]
    public async Task Api_keys_and_webhook_secrets_are_returned_once_and_only_hashes_persist()
    {
        var key = await _service.CreateApiKeyAsync(_actor, null, new("ERP", ["orders.read", "inventory.read"], DateTime.UtcNow.AddDays(30)));
        Assert.StartsWith("tk_live_", key.Secret); Assert.DoesNotContain(key.Secret!, (await _db.SystemApiKeys.SingleAsync()).KeyHash);
        Assert.Null((await _service.DashboardAsync()).ApiKeys.Single().Secret);
        await _service.RevokeApiKeyAsync(_actor, null, key.Id);
        Assert.False((await _service.DashboardAsync()).ApiKeys.Single().IsActive);

        var hook = await _service.SaveWebhookAsync(_actor, null, null, new("order.created", "https://example.com/hooks/orders", true));
        Assert.StartsWith("whsec_", hook.Secret); Assert.DoesNotContain(hook.Secret!, (await _db.SystemWebhooks.SingleAsync()).SecretHash);
        Assert.Null((await _service.DashboardAsync()).Webhooks.Single().Secret);
    }

    [Fact]
    public async Task Resources_and_audits_are_persisted()
    {
        await _service.SaveZoneAsync(_actor, null, null, new("القاهرة", "القاهرة", "التجمع، مدينة نصر", 75, 2, 1.5m, 2, true));
        await _service.SaveBankAsync(_actor, null, null, new("بنك مصر", "مهندسيتو", "123456", "EG120001", "EGP", true, true));
        await _service.SaveTranslationAsync(_actor, null, new(null, "settings.save", "حفظ", "Save", "Enregistrer"));
        var dashboard = await _service.DashboardAsync();
        Assert.Single(dashboard.DeliveryZones); Assert.Single(dashboard.BankAccounts); Assert.Single(dashboard.Translations);
        Assert.Equal(3, await _db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Manual_backup_creates_a_nonempty_sqlite_copy()
    {
        _db.SystemTranslations.Add(new SystemTranslation { Key = "backup.test", Arabic = "اختبار", English = "Test" });
        await _db.SaveChangesAsync();
        var backup = await _service.CreateBackupAsync(_actor, "127.0.0.1");
        Assert.Equal("Completed", backup.Status); Assert.True(backup.SizeBytes > 0);
        var stored = await _db.SystemBackups.SingleAsync();
        Assert.True(File.Exists(stored.StoragePath)); Assert.Equal(64, stored.Sha256!.Length);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
