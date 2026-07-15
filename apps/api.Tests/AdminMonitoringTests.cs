using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.AdminMonitoring;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminMonitoringTests : IDisposable
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

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mohandseto-monitoring-{Guid.NewGuid():N}");
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminSystemSettingsService _settings;
    private readonly AdminMonitoringService _service;
    private readonly RuntimeMetrics _runtime = new();
    private readonly Guid _actor = Guid.NewGuid();

    public AdminMonitoringTests()
    {
        Directory.CreateDirectory(_root);
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated();
        var environment = new TestEnvironment(_root);
        _settings = new AdminSystemSettingsService(_db, DataProtectionProvider.Create(Path.Combine(_root, "keys")), environment);
        _service = new AdminMonitoringService(_db, _runtime, _settings, environment);
    }

    [Fact]
    public async Task Dashboard_uses_runtime_and_persisted_operational_data()
    {
        _runtime.Record(125, 200); _runtime.Record(640, 500);
        _db.SystemErrorEvents.Add(new SystemErrorEvent { Number = "ERR-TEST", Fingerprint = "test", Service = "API", Message = "boom", OccurrenceCount = 3 });
        await _db.SaveChangesAsync();

        var dashboard = await _service.DashboardAsync();

        Assert.Equal(2, dashboard.Health.RequestsPerMinute);
        Assert.Equal(3, dashboard.Health.Errors24Hours);
        Assert.Equal(4, dashboard.Queues.Count);
        Assert.Contains(dashboard.Services, x => x.Code == "database" && x.Status == "Healthy");
        Assert.Single(dashboard.Errors.Items);
    }

    [Fact]
    public async Task Error_resolution_and_ip_blocking_are_audited()
    {
        var error = new SystemErrorEvent { Number = "ERR-1", Fingerprint = "fingerprint", Message = "failure" };
        _db.SystemErrorEvents.Add(error); await _db.SaveChangesAsync();

        var resolved = await _service.ResolveErrorAsync(_actor, "10.0.0.1", error.Id, new("تم إصلاح المصدر"));
        var blocked = await _service.BlockIpAsync(_actor, null, new("203.0.113.7", "محاولات دخول آلية", "خارج مصر", 8));

        Assert.NotNull(resolved.ResolvedAt); Assert.True(blocked.IsActive);
        Assert.True(await _db.BlockedIpAddresses.AnyAsync(x => x.IpAddress == "203.0.113.7" && x.IsActive));
        Assert.Equal(2, await _db.AuditLogs.CountAsync());
        await _service.UnblockIpAsync(_actor, null, blocked.Id);
        Assert.False((await _db.BlockedIpAddresses.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Repeated_failed_logins_create_one_reviewable_security_alert()
    {
        for (var i = 0; i < 6; i++) _db.LoginAudits.Add(new LoginAudit { Identifier = "admin@example.com", IpAddress = "198.51.100.9", Succeeded = false, FailureReason = "invalid" });
        await _db.SaveChangesAsync();

        var first = await _service.DashboardAsync();
        var second = await _service.DashboardAsync();

        var alert = Assert.Single(first.SuspiciousActivities);
        Assert.Single(second.SuspiciousActivities);
        var reviewed = await _service.InvestigateAsync(_actor, null, alert.Id, new("مراجعة سجلات المصادقة"));
        Assert.Equal("Investigating", reviewed.Status);
    }

    [Fact]
    public async Task Restore_request_requires_maintenance_and_verifies_backup_checksum()
    {
        var backup = await _settings.CreateBackupAsync(_actor, null);
        await Assert.ThrowsAsync<ApiException>(() => _service.RequestRestoreAsync(_actor, null, backup.Id, new("production", "استرجاع بيانات تشغيلية سليمة", "RESTORE")));
        _db.MobileAppConfigs.Add(new MobileAppConfig { Platform = "all", MaintenanceEnabled = true }); await _db.SaveChangesAsync();

        var request = await _service.RequestRestoreAsync(_actor, "10.0.0.1", backup.Id, new("production", "استرجاع بيانات تشغيلية سليمة", "RESTORE"));

        Assert.Equal("AwaitingMaintenanceRestart", request.Status);
        Assert.Equal(64, (await _db.SystemRestoreRequests.SingleAsync()).VerifiedSha256.Length);
    }

    [Fact]
    public async Task Feature_flags_support_runtime_evaluation_and_soft_delete()
    {
        var user = Guid.NewGuid();
        var flag = await _service.SaveFlagAsync(_actor, null, null, new("quotes.compare", "مقارنة العروض", "مقارنة العروض جنبًا إلى جنب", true, "AllUsers", 100, null, null, null, null));
        var evaluated = await _service.EvaluateFlagsAsync(user, null);
        Assert.True(evaluated.Flags["quotes.compare"]);

        var updated = await _service.SaveFlagAsync(_actor, null, flag.Id, new(flag.Key, flag.NameAr, flag.DescriptionAr, false, flag.Scope, flag.RolloutPercent, flag.TargetTenantIds, flag.TargetUserIds, null, null));
        Assert.False(updated.IsEnabled);
        await _service.DeleteFlagAsync(_actor, null, flag.Id);
        Assert.Empty((await _service.DashboardAsync()).FeatureFlags);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
