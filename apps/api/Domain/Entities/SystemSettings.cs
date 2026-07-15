using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class SystemSetting : BaseEntity
{
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
}

public class SystemBankAccount : BaseEntity
{
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string Currency { get; set; } = "EGP";
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SystemApiKey : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string ScopesCsv { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);
}

public class SystemWebhook : BaseEntity
{
    public string Event { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; }
}

public class SystemTranslation : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Arabic { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
    public string? French { get; set; }
}

public class IntegrationConnection : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProtectedConfigJson { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Environment { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public DateTime? NextSyncAt { get; set; }
}

public enum IntegrationOperationStatus { Processing, Succeeded, Failed, Retrying }
public class IntegrationOperationLog : BaseEntity
{
    public string Integration { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public IntegrationOperationStatus Status { get; set; }
    public int Attempt { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
    public int DurationMs { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
    public string? Endpoint { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsRetryable { get; set; } = true;
    public DateTime? NextRetryAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum SystemBackupStatus { Processing, Completed, Failed }
public class SystemBackup : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public SystemBackupStatus Status { get; set; }
    public bool IsAutomatic { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
