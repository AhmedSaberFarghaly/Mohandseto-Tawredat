using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminMonitoring;

public sealed class AdminMonitoringService(
    AppDbContext db,
    RuntimeMetrics runtime,
    AdminSystemSettingsService settings,
    IWebHostEnvironment environment)
{
    public async Task<MonitoringDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        await DetectSuspiciousLoginsAsync(ct);
        var now = DateTime.UtcNow;
        var samples = runtime.Snapshot();
        var recent = samples.Where(x => x.At >= now.AddMinutes(-1)).ToList();
        var day = samples.Where(x => x.At >= now.AddHours(-24)).ToList();
        var uptimeSeconds = Math.Max(0, (long)(now - runtime.StartedAt).TotalSeconds);
        var activeUsers = await db.RefreshTokens.AsNoTracking().CountAsync(x => x.RevokedAt == null && x.ExpiresAt > now && x.LastSeenAt >= now.AddMinutes(-30), ct);
        var errors24 = await db.SystemErrorEvents.AsNoTracking().Where(x => x.LastOccurredAt >= now.AddHours(-24)).SumAsync(x => (int?)x.OccurrenceCount, ct) ?? 0;
        var uptime = day.Count == 0 ? 100m : Math.Round(day.Count(x => x.StatusCode < 500) * 100m / day.Count, 2);
        var health = new HealthOverviewDto(uptime, uptimeSeconds, day.Count == 0 ? 0 : Math.Round(day.Average(x => x.DurationMs), 1),
            activeUsers, recent.Count, errors24, now, Trend(day));

        var database = await DatabaseAsync(ct);
        var storage = await StorageAsync(ct);
        var services = await ServicesAsync(database, storage, ct);
        var queues = await QueuesAsync(ct);
        var errors = await ErrorsAsync(null, null, null, null, null, 1, 25, ct);
        var failedLogins = await FailedLoginsAsync(ct);
        var suspicious = (await db.SuspiciousActivities.AsNoTracking().OrderByDescending(x => x.DetectedAt).Take(100).ToListAsync(ct)).Select(Activity).ToList();
        var blocked = (await db.BlockedIpAddresses.AsNoTracking().OrderByDescending(x => x.BlockedAt).ToListAsync(ct)).Select(Blocked).ToList();
        var backupRows = await db.SystemBackups.AsNoTracking().OrderByDescending(x => x.StartedAt).Take(100).ToListAsync(ct);
        var schedule = await settings.ValueAsync("backup", "schedule", "يوميًا 02:00 ص", ct);
        var backups = new BackupOverviewDto(backupRows.FirstOrDefault(x => x.Status == SystemBackupStatus.Completed)?.CompletedAt,
            backupRows.Where(x => x.Status == SystemBackupStatus.Completed).Sum(x => x.SizeBytes), backupRows.Count,
            schedule, backupRows.Select(Backup).ToList());
        var restores = (await db.SystemRestoreRequests.AsNoTracking().Include(x => x.Backup).OrderByDescending(x => x.RequestedAt).Take(50).ToListAsync(ct)).Select(Restore).ToList();
        var versions = await VersionsAsync(ct);
        var flags = (await db.FeatureFlags.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct)).Select(Flag).ToList();
        return new(health, database, storage, services, queues, errors, failedLogins, suspicious, blocked, backups, restores, versions, flags);
    }

    public async Task<ErrorPageDto> ErrorsAsync(string? search, string? severity, string? service, DateTime? from,
        DateTime? to, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.SystemErrorEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Message.Contains(search) || x.Fingerprint.Contains(search));
        if (Enum.TryParse<SystemErrorSeverity>(severity, true, out var parsed)) query = query.Where(x => x.Severity == parsed);
        if (!string.IsNullOrWhiteSpace(service)) query = query.Where(x => x.Service == service);
        if (from is not null) query = query.Where(x => x.LastOccurredAt >= from.Value);
        if (to is not null) query = query.Where(x => x.LastOccurredAt < to.Value.Date.AddDays(1));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.LastOccurredAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(total, page, pageSize, rows.Select(Error).ToList());
    }

    public async Task<ErrorEventDto> ErrorAsync(Guid id, CancellationToken ct = default) =>
        Error(await db.SystemErrorEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
              ?? throw ApiException.NotFound("الخطأ غير موجود"));

    public async Task<ErrorEventDto> ResolveErrorAsync(Guid actor, string? ip, Guid id, ResolveErrorDto dto, CancellationToken ct = default)
    {
        var row = await db.SystemErrorEvents.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الخطأ غير موجود");
        row.ResolvedAt = DateTime.UtcNow; row.ResolvedBy = actor; row.ResolutionNote = Clean(dto.Note, 1000); row.UpdatedBy = actor;
        Audit(actor, ip, "monitoring.error_resolved", nameof(SystemErrorEvent), id.ToString(), new { dto.Note });
        await db.SaveChangesAsync(ct); return Error(row);
    }

    public async Task<BlockedIpDto> BlockIpAsync(Guid actor, string? actorIp, BlockIpDto dto, CancellationToken ct = default)
    {
        if (!IPAddress.TryParse(dto.IpAddress?.Trim(), out var address)) throw ApiException.BadRequest("عنوان IP غير صالح");
        if (IPAddress.IsLoopback(address)) throw ApiException.BadRequest("لا يمكن حظر عنوان loopback");
        if (string.Equals(address.ToString(), actorIp, StringComparison.OrdinalIgnoreCase)) throw ApiException.BadRequest("لا يمكن حظر عنوان جلستك الحالية");
        var reason = CleanRequired(dto.Reason, 8, 500, "سبب الحظر");
        var normalized = address.ToString();
        var row = await db.BlockedIpAddresses.FirstOrDefaultAsync(x => x.IpAddress == normalized, ct);
        if (row is null) { row = new BlockedIpAddress { IpAddress = normalized, CreatedBy = actor }; db.BlockedIpAddresses.Add(row); }
        row.Reason = reason; row.Location = Clean(dto.Location, 150); row.FailedAttempts = Math.Max(0, dto.FailedAttempts);
        row.BlockedAt = DateTime.UtcNow; row.ExpiresAt = dto.ExpiresAt; row.IsActive = true; row.BlockedBy = actor;
        row.UnblockedAt = null; row.UnblockedBy = null; row.UpdatedBy = actor;
        Audit(actor, actorIp, "security.ip_blocked", nameof(BlockedIpAddress), normalized, new { reason, dto.ExpiresAt });
        await db.SaveChangesAsync(ct); return Blocked(row);
    }

    public async Task UnblockIpAsync(Guid actor, string? ip, Guid id, CancellationToken ct = default)
    {
        var row = await db.BlockedIpAddresses.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("عنوان IP غير موجود");
        row.IsActive = false; row.UnblockedAt = DateTime.UtcNow; row.UnblockedBy = actor; row.UpdatedBy = actor;
        Audit(actor, ip, "security.ip_unblocked", nameof(BlockedIpAddress), row.IpAddress, null); await db.SaveChangesAsync(ct);
    }

    public Task<SuspiciousActivityDto> InvestigateAsync(Guid actor, string? ip, Guid id, ReviewSuspiciousActivityDto dto, CancellationToken ct = default) =>
        ReviewActivityAsync(actor, ip, id, SuspiciousActivityStatus.Investigating, dto.Note, ct);
    public Task<SuspiciousActivityDto> IgnoreAsync(Guid actor, string? ip, Guid id, ReviewSuspiciousActivityDto dto, CancellationToken ct = default) =>
        ReviewActivityAsync(actor, ip, id, SuspiciousActivityStatus.Ignored, dto.Note, ct);

    private async Task<SuspiciousActivityDto> ReviewActivityAsync(Guid actor, string? ip, Guid id, SuspiciousActivityStatus status, string? note, CancellationToken ct)
    {
        var row = await db.SuspiciousActivities.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("النشاط المشبوه غير موجود");
        row.Status = status; row.ReviewedAt = DateTime.UtcNow; row.ReviewedBy = actor; row.ReviewNote = Clean(note, 1000); row.UpdatedBy = actor;
        Audit(actor, ip, status == SuspiciousActivityStatus.Ignored ? "security.activity_ignored" : "security.activity_investigating", nameof(SuspiciousActivity), id.ToString(), new { note });
        await db.SaveChangesAsync(ct); return Activity(row);
    }

    public Task<SettingsBackupDto> CreateBackupAsync(Guid actor, string? ip, CancellationToken ct = default) => settings.CreateBackupAsync(actor, ip, false, ct);

    public async Task<RestoreRequestDto> RequestRestoreAsync(Guid actor, string? ip, Guid backupId, CreateRestoreRequestDto dto, CancellationToken ct = default)
    {
        if (!string.Equals(dto.Confirmation?.Trim(), "RESTORE", StringComparison.Ordinal)) throw ApiException.BadRequest("اكتب RESTORE لتأكيد طلب الاستعادة");
        var reason = CleanRequired(dto.Reason, 10, 1000, "سبب الاستعادة");
        var target = dto.Environment?.Trim().ToLowerInvariant();
        if (target is not ("production" or "staging")) throw ApiException.BadRequest("البيئة يجب أن تكون production أو staging");
        var maintenance = await db.MobileAppConfigs.AsNoTracking().AnyAsync(x => x.MaintenanceEnabled, ct);
        if (!maintenance) throw ApiException.Conflict("فعّل وضع الصيانة قبل طلب الاستعادة");
        var backup = await db.SystemBackups.FirstOrDefaultAsync(x => x.Id == backupId && x.Status == SystemBackupStatus.Completed, ct)
                     ?? throw ApiException.NotFound("النسخة الاحتياطية المكتملة غير موجودة");
        if (!File.Exists(backup.StoragePath)) throw ApiException.Conflict("ملف النسخة الاحتياطية غير متاح في التخزين");
        await using var stream = File.OpenRead(backup.StoragePath);
        var sha = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(backup.Sha256) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(sha), Encoding.ASCII.GetBytes(backup.Sha256.ToLowerInvariant())))
            throw ApiException.Conflict("فشل التحقق من سلامة النسخة الاحتياطية");
        var row = new SystemRestoreRequest { BackupId = backupId, Backup = backup, Environment = target, Reason = reason,
            Status = RestoreRequestStatus.AwaitingMaintenanceRestart, VerifiedSha256 = sha, RequestedBy = actor, CreatedBy = actor };
        db.SystemRestoreRequests.Add(row);
        Audit(actor, ip, "monitoring.restore_requested", nameof(SystemRestoreRequest), row.Id.ToString(), new { backupId, target, sha });
        await db.SaveChangesAsync(ct); return Restore(row);
    }

    public async Task<SystemVersionDto> SaveVersionAsync(Guid actor, string? ip, SaveSystemVersionDto dto, CancellationToken ct = default)
    {
        var version = CleanRequired(dto.Version, 1, 40, "رقم الإصدار");
        if (!Regex.IsMatch(version, @"^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][A-Za-z0-9.-]+)?$")) throw ApiException.BadRequest("رقم الإصدار يجب أن يتبع SemVer");
        if (await db.SystemVersions.AnyAsync(x => x.Version == version, ct)) throw ApiException.Conflict("الإصدار مسجل بالفعل");
        var row = new SystemVersion { Version = version, TitleAr = CleanRequired(dto.TitleAr, 3, 200, "عنوان الإصدار"), NotesAr = Clean(dto.NotesAr, 4000),
            Environment = CleanRequired(dto.Environment, 2, 30, "البيئة"), CommitSha = Clean(dto.CommitSha, 64), IsStable = dto.IsStable,
            ReleasedAt = dto.ReleasedAt ?? DateTime.UtcNow, ReleasedBy = actor, CreatedBy = actor };
        db.SystemVersions.Add(row); Audit(actor, ip, "monitoring.version_registered", nameof(SystemVersion), version, null);
        await db.SaveChangesAsync(ct); return Version(row);
    }

    public async Task<FeatureFlagDto> SaveFlagAsync(Guid actor, string? ip, Guid? id, SaveFeatureFlagDto dto, CancellationToken ct = default)
    {
        var key = dto.Key?.Trim().ToLowerInvariant() ?? "";
        if (!Regex.IsMatch(key, "^[a-z][a-z0-9._-]{2,79}$")) throw ApiException.BadRequest("مفتاح الميزة غير صالح");
        if (!Enum.TryParse<FeatureFlagScope>(dto.Scope, true, out var scope)) throw ApiException.BadRequest("نطاق الميزة غير صالح");
        if (dto.RolloutPercent is < 0 or > 100) throw ApiException.BadRequest("نسبة الإطلاق يجب أن تكون بين 0 و100");
        if (dto.StartsAt is not null && dto.EndsAt <= dto.StartsAt) throw ApiException.BadRequest("تاريخ نهاية الميزة يجب أن يلي تاريخ بدايتها");
        if (scope == FeatureFlagScope.Tenant && (dto.TargetTenantIds is null || dto.TargetTenantIds.Count == 0)) throw ApiException.BadRequest("اختر شركة واحدة على الأقل لهذا النطاق");
        if (scope == FeatureFlagScope.User && (dto.TargetUserIds is null || dto.TargetUserIds.Count == 0)) throw ApiException.BadRequest("اختر مستخدمًا واحدًا على الأقل لهذا النطاق");
        var duplicate = await db.FeatureFlags.AnyAsync(x => x.Key == key && (!id.HasValue || x.Id != id.Value), ct);
        if (duplicate) throw ApiException.Conflict("مفتاح الميزة مستخدم بالفعل");
        var row = id.HasValue ? await db.FeatureFlags.FirstOrDefaultAsync(x => x.Id == id.Value, ct) ?? throw ApiException.NotFound("الميزة غير موجودة") : new FeatureFlag { CreatedBy = actor };
        if (!id.HasValue) db.FeatureFlags.Add(row);
        row.Key = key; row.NameAr = CleanRequired(dto.NameAr, 3, 150, "اسم الميزة"); row.DescriptionAr = CleanRequired(dto.DescriptionAr, 3, 500, "وصف الميزة");
        row.IsEnabled = dto.IsEnabled; row.Scope = scope; row.RolloutPercent = dto.RolloutPercent;
        row.TargetTenantIdsCsv = Csv(dto.TargetTenantIds); row.TargetUserIdsCsv = Csv(dto.TargetUserIds); row.StartsAt = dto.StartsAt; row.EndsAt = dto.EndsAt; row.UpdatedBy = actor;
        Audit(actor, ip, id.HasValue ? "monitoring.feature_flag_updated" : "monitoring.feature_flag_created", nameof(FeatureFlag), row.Id.ToString(), new { key, scope, dto.RolloutPercent, dto.IsEnabled });
        await db.SaveChangesAsync(ct); return Flag(row);
    }

    public async Task DeleteFlagAsync(Guid actor, string? ip, Guid id, CancellationToken ct = default)
    {
        var row = await db.FeatureFlags.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الميزة غير موجودة");
        db.FeatureFlags.Remove(row); Audit(actor, ip, "monitoring.feature_flag_deleted", nameof(FeatureFlag), id.ToString(), new { row.Key }); await db.SaveChangesAsync(ct);
    }

    public async Task<EvaluatedFeatureFlagsDto> EvaluateFlagsAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var flags = await db.FeatureFlags.AsNoTracking().Where(x => (x.StartsAt == null || x.StartsAt <= now) && (x.EndsAt == null || x.EndsAt > now)).ToListAsync(ct);
        return new(flags.ToDictionary(x => x.Key, x => EnabledFor(x, userId, tenantId)), now);
    }

    private async Task<DatabaseHealthDto> DatabaseAsync(CancellationToken ct)
    {
        var watch = Stopwatch.StartNew(); var status = "Healthy";
        try { await db.Database.ExecuteSqlRawAsync("SELECT 1", ct); } catch { status = "Down"; }
        watch.Stop(); var connection = db.Database.GetDbConnection(); long size = 0;
        if (!string.IsNullOrWhiteSpace(connection.DataSource) && File.Exists(connection.DataSource)) size = new FileInfo(connection.DataSource).Length;
        var backup = await db.SystemBackups.AsNoTracking().Where(x => x.Status == SystemBackupStatus.Completed).MaxAsync(x => (DateTime?)x.CompletedAt, ct);
        var available = DriveAvailable(environment.ContentRootPath);
        var samples = runtime.DatabaseSnapshot();
        var slow = samples.Count(x => x.DurationMs >= 500);
        return new(connection.GetType().Name.Replace("Connection", ""), status, connection.State == System.Data.ConnectionState.Open ? 1 : 0,
            1, samples.Count == 0 ? Math.Round(watch.Elapsed.TotalMilliseconds, 1) : Math.Round(samples.Average(x => x.DurationMs), 1), size, slow,
            samples.Count == 0 ? 100 : Math.Round(samples.Count(x => x.Succeeded) * 100m / samples.Count, 2), backup, available, DatabaseTrend(samples));
    }

    private async Task<StorageHealthDto> StorageAsync(CancellationToken ct)
    {
        var capacity = DriveCapacity(environment.ContentRootPath); var available = DriveAvailable(environment.ContentRootPath); var used = Math.Max(0, capacity - available);
        var dbPath = db.Database.GetDbConnection().DataSource;
        var categories = new List<StorageCategoryDto>
        {
            new("database", "قاعدة البيانات", File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0, "blue"),
            new("product-images", "صور المنتجات", DirectoryBytes(Path.Combine(environment.ContentRootPath, "storage", "products")), "purple"),
            new("documents", "المستندات", DirectoryBytes(Path.Combine(environment.ContentRootPath, "storage", "documents")), "orange"),
            new("backups", "النسخ الاحتياطية", DirectoryBytes(Path.Combine(environment.ContentRootPath, "storage", "backups")), "teal"),
        };
        var thresholdRaw = await settings.ValueAsync("storage", "warningThreshold", "80", ct);
        var threshold = int.TryParse(thresholdRaw, out var parsed) ? Math.Clamp(parsed, 1, 99) : 80;
        var auto = bool.TryParse(await settings.ValueAsync("storage", "autoExpand", "false", ct), out var value) && value;
        return new(capacity, used, available, capacity == 0 ? 0 : Math.Round(used * 100m / capacity, 2), threshold, auto, categories);
    }

    private async Task<IReadOnlyList<ServiceHealthDto>> ServicesAsync(DatabaseHealthDto database, StorageHealthDto storage, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var integrations = await db.IntegrationConnections.AsNoTracking().ToListAsync(ct);
        var integrationHealthy = integrations.Count == 0 || integrations.All(x => !x.IsEnabled || x.IsConnected);
        var backupAt = await db.SystemBackups.AsNoTracking().Where(x => x.Status == SystemBackupStatus.Completed).MaxAsync(x => (DateTime?)x.CompletedAt, ct);
        return
        [
            new("api", "واجهة API الرئيسية", "Healthy", 100, runtime.Snapshot().LastOrDefault()?.DurationMs ?? 0, now, null),
            new("database", "قاعدة البيانات", database.Status, database.SuccessRate, database.QueryLatencyMs, now, database.Provider),
            new("storage", "التخزين", storage.UsagePercent >= storage.WarningThresholdPercent ? "Degraded" : "Healthy", 100, 0, now, $"{storage.UsagePercent}% مستخدم"),
            new("integrations", "التكاملات الخارجية", integrationHealthy ? "Healthy" : "Degraded", integrations.Count == 0 ? 100 : Math.Round(integrations.Count(x => !x.IsEnabled || x.IsConnected) * 100m / integrations.Count, 2), 0, now, $"{integrations.Count(x => x.IsConnected)} متصل"),
            new("backups", "خدمة النسخ الاحتياطي", backupAt is null || backupAt < now.AddDays(-2) ? "Degraded" : "Healthy", backupAt is null ? 0 : 100, 0, now, backupAt?.ToString("u")),
        ];
    }

    private async Task<IReadOnlyList<QueueHealthDto>> QueuesAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var notificationWaiting = await db.ContentDispatches.AsNoTracking().CountAsync(x => x.Status == ContentDispatchStatus.Scheduled, ct);
        var notificationDone = await db.ContentDispatches.AsNoTracking().CountAsync(x => x.Status == ContentDispatchStatus.Sent && x.SentAt >= today, ct);
        var invoiceRows = await db.IntegrationOperationLogs.AsNoTracking().Where(x => x.Integration == "einvoice").ToListAsync(ct);
        var erpRows = await db.IntegrationOperationLogs.AsNoTracking().Where(x => x.Integration == "erp").ToListAsync(ct);
        var emailWaiting = await db.MarketingDeliveries.AsNoTracking().CountAsync(x => x.Status == MarketingDeliveryStatus.Queued, ct);
        var emailDone = await db.MarketingDeliveries.AsNoTracking().CountAsync(x => x.Status == MarketingDeliveryStatus.Delivered && x.DeliveredAt >= today, ct);
        var emailFailed = await db.MarketingDeliveries.AsNoTracking().CountAsync(x => x.Status == MarketingDeliveryStatus.Failed, ct);
        static QueueHealthDto IntegrationQueue(string code, string name, IReadOnlyCollection<IntegrationOperationLog> rows, DateTime today)
        {
            var waiting = rows.Count(x => x.Status is IntegrationOperationStatus.Processing or IntegrationOperationStatus.Retrying);
            var failed = rows.Count(x => x.Status == IntegrationOperationStatus.Failed);
            return new(code, name, waiting, rows.Count(x => x.Status == IntegrationOperationStatus.Processing), rows.Count(x => x.Status == IntegrationOperationStatus.Succeeded && x.CompletedAt >= today), failed, failed > 0 || waiting > 25 ? "Delayed" : "Healthy");
        }
        return
        [
            new("notifications", "الإشعارات", notificationWaiting, 0, notificationDone, 0, notificationWaiting > 25 ? "Delayed" : "Healthy"),
            IntegrationQueue("invoices", "الفواتير الإلكترونية", invoiceRows, today),
            IntegrationQueue("erp", "مزامنة ERP", erpRows, today),
            new("email", "البريد الإلكتروني", emailWaiting, 0, emailDone, emailFailed, emailFailed > 0 || emailWaiting > 25 ? "Delayed" : "Healthy"),
        ];
    }

    private async Task<IReadOnlyList<FailedLoginDto>> FailedLoginsAsync(CancellationToken ct)
    {
        var rows = await db.LoginAudits.AsNoTracking().Where(x => !x.Succeeded && x.CreatedAt >= DateTime.UtcNow.AddHours(-24)).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var blocked = await db.BlockedIpAddresses.AsNoTracking().Where(x => x.IsActive).Select(x => x.IpAddress).ToListAsync(ct);
        return rows.GroupBy(x => new { x.Identifier, Ip = x.IpAddress ?? "unknown" }).Select(g => new FailedLoginDto(g.Key.Identifier, g.Key.Ip,
            g.First().Location, g.Count(), g.Max(x => x.CreatedAt), g.First().FailureReason, blocked.Contains(g.Key.Ip))).OrderByDescending(x => x.LastAttemptAt).ToList();
    }

    private async Task DetectSuspiciousLoginsAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var failed = await db.LoginAudits.AsNoTracking().Where(x => !x.Succeeded && x.CreatedAt >= since && x.IpAddress != null).ToListAsync(ct);
        var groups = failed.GroupBy(x => x.IpAddress!).Where(x => x.Count() >= 5).ToList();
        var changed = false;
        foreach (var group in groups)
        {
            var fingerprint = $"failed-login:{group.Key}:{DateTime.UtcNow:yyyyMMdd}";
            if (await db.SuspiciousActivities.AnyAsync(x => x.Fingerprint == fingerprint && x.Status != SuspiciousActivityStatus.Resolved, ct)) continue;
            db.SuspiciousActivities.Add(new SuspiciousActivity { Fingerprint = fingerprint, Type = "RepeatedFailedLogin", Severity = group.Count() >= 10 ? SuspiciousActivitySeverity.Critical : SuspiciousActivitySeverity.High,
                TitleAr = "محاولات دخول متكررة", DescriptionAr = $"تم رصد {group.Count()} محاولات فاشلة خلال 24 ساعة", Identifier = group.First().Identifier, IpAddress = group.Key }); changed = true;
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<SystemVersionDto>> VersionsAsync(CancellationToken ct)
    {
        var rows = await db.SystemVersions.AsNoTracking().OrderByDescending(x => x.ReleasedAt).ToListAsync(ct);
        if (rows.Count > 0) return rows.Select(Version).ToList();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
        return [new(Guid.Empty, version, "الإصدار الحالي", "الإصدار المنشور حاليًا للتطبيق", environment.EnvironmentName, null, true, File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location))];
    }

    private static bool EnabledFor(FeatureFlag flag, Guid userId, Guid? tenantId)
    {
        if (!flag.IsEnabled) return false;
        return flag.Scope switch
        {
            FeatureFlagScope.AllUsers => true,
            FeatureFlagScope.Tenant => tenantId.HasValue && ParseCsv(flag.TargetTenantIdsCsv).Contains(tenantId.Value),
            FeatureFlagScope.User => ParseCsv(flag.TargetUserIdsCsv).Contains(userId),
            FeatureFlagScope.Percentage => PercentageBucket(flag.Key, userId) < flag.RolloutPercent,
            _ => false,
        };
    }

    private static int PercentageBucket(string key, Guid userId) => (int)(BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes($"{key}:{userId:N}")), 0) % 100);
    private static IReadOnlyList<MetricPointDto> Trend(IEnumerable<RuntimeRequestSample> rows) => rows.GroupBy(x => new DateTime(x.At.Year, x.At.Month, x.At.Day, x.At.Hour, x.At.Minute / 10 * 10, 0, DateTimeKind.Utc)).OrderBy(x => x.Key).Select(x => new MetricPointDto(x.Key, Math.Round(x.Average(y => y.DurationMs), 1))).ToList();
    private static IReadOnlyList<MetricPointDto> DatabaseTrend(IEnumerable<RuntimeDatabaseSample> rows) => rows.GroupBy(x => new DateTime(x.At.Year, x.At.Month, x.At.Day, x.At.Hour, x.At.Minute / 10 * 10, 0, DateTimeKind.Utc)).OrderBy(x => x.Key).Select(x => new MetricPointDto(x.Key, Math.Round(x.Average(y => y.DurationMs), 1))).ToList();
    private static long DirectoryBytes(string path) { try { return Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(x => { try { return new FileInfo(x).Length; } catch { return 0L; } }) : 0; } catch { return 0; } }
    private static long DriveCapacity(string path) { try { return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).TotalSize; } catch { return 0; } }
    private static long DriveAvailable(string path) { try { return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace; } catch { return 0; } }
    private static string CleanRequired(string? value, int min, int max, string field) { var cleaned = Clean(value, max); if (cleaned is null || cleaned.Length < min) throw ApiException.BadRequest($"{field} مطلوب"); return cleaned; }
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var cleaned = value.Trim().Replace("\0", ""); return cleaned[..Math.Min(cleaned.Length, max)]; }
    private static string? Csv(IReadOnlyList<Guid>? values) => values is null || values.Count == 0 ? null : string.Join(',', values.Distinct());
    private static IReadOnlyList<Guid> ParseCsv(string? csv) => string.IsNullOrWhiteSpace(csv) ? [] : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).ToList();
    private void Audit(Guid actor, string? ip, string action, string entity, string? id, object? data) => db.AuditLogs.Add(new AuditLog { UserId = actor, Ip = ip, Action = action, EntityType = entity, EntityId = id, DataJson = data is null ? null : JsonSerializer.Serialize(data) });
    private static ErrorEventDto Error(SystemErrorEvent x) => new(x.Id, x.Number, x.Severity.ToString(), x.Service, x.Message, x.ExceptionType, x.StackTrace, x.ContextJson, x.CorrelationId, x.Path, x.UserId, x.TenantId, x.OccurrenceCount, x.FirstOccurredAt, x.LastOccurredAt, x.ResolvedAt, x.ResolutionNote);
    private static SuspiciousActivityDto Activity(SuspiciousActivity x) => new(x.Id, x.Type, x.Severity.ToString(), x.TitleAr, x.DescriptionAr, x.Identifier, x.IpAddress, x.Status.ToString(), x.DetectedAt, x.ReviewedAt, x.ReviewNote);
    private static BlockedIpDto Blocked(BlockedIpAddress x) => new(x.Id, x.IpAddress, x.Reason, x.Location, x.FailedAttempts, x.BlockedAt, x.ExpiresAt, x.IsActive, x.UnblockedAt);
    private static BackupDto Backup(SystemBackup x) => new(x.Id, x.FileName, x.SizeBytes, x.Status.ToString(), x.IsAutomatic, x.StartedAt, x.CompletedAt, x.Error, x.Sha256);
    private static RestoreRequestDto Restore(SystemRestoreRequest x) => new(x.Id, x.BackupId, x.Backup.FileName, x.Environment, x.Reason, x.Status.ToString(), x.RequestedAt, x.CompletedAt, x.Error);
    private static SystemVersionDto Version(SystemVersion x) => new(x.Id, x.Version, x.TitleAr, x.NotesAr, x.Environment, x.CommitSha, x.IsStable, x.ReleasedAt);
    private static FeatureFlagDto Flag(FeatureFlag x) => new(x.Id, x.Key, x.NameAr, x.DescriptionAr, x.IsEnabled, x.Scope.ToString(), x.RolloutPercent, ParseCsv(x.TargetTenantIdsCsv), ParseCsv(x.TargetUserIdsCsv), x.StartsAt, x.EndsAt, x.UpdatedAt ?? x.CreatedAt);
}
