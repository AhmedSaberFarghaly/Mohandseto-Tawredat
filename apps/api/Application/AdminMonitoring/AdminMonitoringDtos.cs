namespace Mohandseto.Api.Application.AdminMonitoring;

public sealed record MonitoringDashboardDto(
    HealthOverviewDto Health,
    DatabaseHealthDto Database,
    StorageHealthDto Storage,
    IReadOnlyList<ServiceHealthDto> Services,
    IReadOnlyList<QueueHealthDto> Queues,
    ErrorPageDto Errors,
    IReadOnlyList<FailedLoginDto> FailedLogins,
    IReadOnlyList<SuspiciousActivityDto> SuspiciousActivities,
    IReadOnlyList<BlockedIpDto> BlockedIps,
    BackupOverviewDto Backups,
    IReadOnlyList<RestoreRequestDto> RestoreRequests,
    IReadOnlyList<SystemVersionDto> Versions,
    IReadOnlyList<FeatureFlagDto> FeatureFlags);

public sealed record HealthOverviewDto(decimal UptimePercent, long UptimeSeconds, double AverageResponseMs,
    int ActiveUsers, int RequestsPerMinute, int Errors24Hours, DateTime CheckedAt,
    IReadOnlyList<MetricPointDto> ResponseTrend);
public sealed record MetricPointDto(DateTime At, double Value);
public sealed record ServiceHealthDto(string Code, string NameAr, string Status, decimal UptimePercent,
    double ResponseMs, DateTime CheckedAt, string? Message);
public sealed record DatabaseHealthDto(string Provider, string Status, int ActiveConnections, int MaxConnections,
    double QueryLatencyMs, long SizeBytes, int SlowQueries24Hours, decimal SuccessRate, DateTime? LastBackupAt,
    long AvailableStorageBytes, IReadOnlyList<MetricPointDto> ConnectionTrend);
public sealed record StorageCategoryDto(string Code, string NameAr, long Bytes, string Tone);
public sealed record StorageHealthDto(long CapacityBytes, long UsedBytes, long AvailableBytes, decimal UsagePercent,
    int WarningThresholdPercent, bool AutoExpandEnabled, IReadOnlyList<StorageCategoryDto> Categories);
public sealed record QueueHealthDto(string Code, string NameAr, int Waiting, int Processing, int CompletedToday,
    int Failed, string Status);

public sealed record ErrorEventDto(Guid Id, string Number, string Severity, string Service, string Message,
    string? ExceptionType, string? StackTrace, string? ContextJson, string? CorrelationId, string? Path,
    Guid? UserId, Guid? TenantId, int OccurrenceCount, DateTime FirstOccurredAt, DateTime LastOccurredAt,
    DateTime? ResolvedAt, string? ResolutionNote);
public sealed record ErrorPageDto(int Total, int Page, int PageSize, IReadOnlyList<ErrorEventDto> Items);
public sealed record FailedLoginDto(string Identifier, string IpAddress, string? Location, int Attempts,
    DateTime LastAttemptAt, string? FailureReason, bool IsBlocked);
public sealed record SuspiciousActivityDto(Guid Id, string Type, string Severity, string TitleAr,
    string DescriptionAr, string? Identifier, string? IpAddress, string Status, DateTime DetectedAt,
    DateTime? ReviewedAt, string? ReviewNote);
public sealed record BlockedIpDto(Guid Id, string IpAddress, string Reason, string? Location, int FailedAttempts,
    DateTime BlockedAt, DateTime? ExpiresAt, bool IsActive, DateTime? UnblockedAt);

public sealed record BackupDto(Guid Id, string FileName, long SizeBytes, string Status, bool IsAutomatic,
    DateTime StartedAt, DateTime? CompletedAt, string? Error, string? Sha256);
public sealed record BackupOverviewDto(DateTime? LatestAt, long TotalSizeBytes, int RetainedCount,
    string ScheduleAr, IReadOnlyList<BackupDto> Items);
public sealed record RestoreRequestDto(Guid Id, Guid BackupId, string BackupFileName, string Environment,
    string Reason, string Status, DateTime RequestedAt, DateTime? CompletedAt, string? Error);
public sealed record SystemVersionDto(Guid Id, string Version, string TitleAr, string? NotesAr,
    string Environment, string? CommitSha, bool IsStable, DateTime ReleasedAt);
public sealed record FeatureFlagDto(Guid Id, string Key, string NameAr, string DescriptionAr, bool IsEnabled,
    string Scope, int RolloutPercent, IReadOnlyList<Guid> TargetTenantIds, IReadOnlyList<Guid> TargetUserIds,
    DateTime? StartsAt, DateTime? EndsAt, DateTime UpdatedAt);

public sealed record ResolveErrorDto(string? Note);
public sealed record BlockIpDto(string IpAddress, string Reason, string? Location, int FailedAttempts = 0,
    DateTime? ExpiresAt = null);
public sealed record ReviewSuspiciousActivityDto(string? Note);
public sealed record CreateRestoreRequestDto(string Environment, string Reason, string Confirmation);
public sealed record SaveSystemVersionDto(string Version, string TitleAr, string? NotesAr,
    string Environment, string? CommitSha, bool IsStable, DateTime? ReleasedAt);
public sealed record SaveFeatureFlagDto(string Key, string NameAr, string DescriptionAr, bool IsEnabled,
    string Scope, int RolloutPercent, IReadOnlyList<Guid>? TargetTenantIds, IReadOnlyList<Guid>? TargetUserIds,
    DateTime? StartsAt, DateTime? EndsAt);
public sealed record EvaluatedFeatureFlagsDto(IReadOnlyDictionary<string, bool> Flags, DateTime EvaluatedAt);
