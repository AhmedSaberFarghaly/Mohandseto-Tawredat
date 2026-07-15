using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum SystemErrorSeverity { Information, Warning, Error, Critical }

/// <summary>A sanitized, grouped operational error. Secrets and request bodies must never be stored here.</summary>
public sealed class SystemErrorEvent : BaseEntity
{
    public string Fingerprint { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public SystemErrorSeverity Severity { get; set; } = SystemErrorSeverity.Error;
    public string Service { get; set; } = "API";
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? ContextJson { get; set; }
    public string? CorrelationId { get; set; }
    public string? Path { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstOccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastOccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
}

public sealed class BlockedIpAddress : BaseEntity
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid BlockedBy { get; set; }
    public DateTime? UnblockedAt { get; set; }
    public Guid? UnblockedBy { get; set; }
}

public enum SuspiciousActivitySeverity { Medium, High, Critical }
public enum SuspiciousActivityStatus { Open, Investigating, Ignored, Resolved }

public sealed class SuspiciousActivity : BaseEntity
{
    public string Fingerprint { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public SuspiciousActivitySeverity Severity { get; set; } = SuspiciousActivitySeverity.High;
    public string TitleAr { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string? IpAddress { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public SuspiciousActivityStatus Status { get; set; } = SuspiciousActivityStatus.Open;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
}

public enum RestoreRequestStatus { Validated, AwaitingMaintenanceRestart, Completed, Failed, Cancelled }

/// <summary>Audited restore intent. Execution happens during a controlled maintenance restart.</summary>
public sealed class SystemRestoreRequest : BaseEntity
{
    public Guid BackupId { get; set; }
    public SystemBackup Backup { get; set; } = null!;
    public string Environment { get; set; } = "production";
    public string Reason { get; set; } = string.Empty;
    public RestoreRequestStatus Status { get; set; } = RestoreRequestStatus.Validated;
    public string VerifiedSha256 { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public Guid RequestedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public sealed class SystemVersion : BaseEntity
{
    public string Version { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? NotesAr { get; set; }
    public string Environment { get; set; } = "production";
    public string? CommitSha { get; set; }
    public bool IsStable { get; set; } = true;
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public Guid ReleasedBy { get; set; }
}

public enum FeatureFlagScope { AllUsers, Percentage, Tenant, User }

public sealed class FeatureFlag : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public FeatureFlagScope Scope { get; set; } = FeatureFlagScope.AllUsers;
    public int RolloutPercent { get; set; } = 100;
    public string? TargetTenantIdsCsv { get; set; }
    public string? TargetUserIdsCsv { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
