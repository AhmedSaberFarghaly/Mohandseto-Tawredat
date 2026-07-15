namespace Mohandseto.Api.Application.AdminSystemSettings;

public sealed record SettingFieldDto(string Code, string LabelAr, string Type, bool Required,
    IReadOnlyList<string> Options, string? Help = null, bool Sensitive = false);
public sealed record SettingSectionDto(string Code, string NameAr, string NameEn, string Category, string Icon,
    string DescriptionAr, IReadOnlyList<SettingFieldDto> Fields, IReadOnlyDictionary<string, string> Values);
public sealed record SettingsDeliveryZoneDto(Guid Id, string Name, string Governorate, string? Cities, decimal BaseFee,
    decimal FeePerKg, decimal FeePerKm, int EstimatedDays, bool IsActive);
public sealed record SettingsBankAccountDto(Guid Id, string BankName, string AccountName, string AccountNumber,
    string? Iban, string Currency, bool IsPrimary, bool IsActive);
public sealed record SettingsApiKeyDto(Guid Id, string Name, string MaskedKey, IReadOnlyList<string> Scopes,
    bool IsActive, DateTime? ExpiresAt, DateTime? LastUsedAt, DateTime CreatedAt, string? Secret = null);
public sealed record SettingsWebhookDto(Guid Id, string Event, string Url, bool IsActive, DateTime? LastTriggeredAt,
    int FailureCount, DateTime CreatedAt, string? Secret = null);
public sealed record SettingsTranslationDto(Guid Id, string Key, string Arabic, string English, string? French);
public sealed record SettingsIntegrationLogDto(Guid Id, string Integration, string Operation, string? Reference,
    string Status, int Attempt, int DurationMs, string? Error, DateTime StartedAt, DateTime? CompletedAt);
public sealed record SettingsBackupDto(Guid Id, string FileName, long SizeBytes, string Status, bool IsAutomatic,
    DateTime StartedAt, DateTime? CompletedAt, string? Error);
public sealed record SystemSettingsDashboardDto(IReadOnlyList<SettingSectionDto> Sections,
    IReadOnlyList<SettingsDeliveryZoneDto> DeliveryZones, IReadOnlyList<SettingsBankAccountDto> BankAccounts,
    IReadOnlyList<SettingsApiKeyDto> ApiKeys, IReadOnlyList<SettingsWebhookDto> Webhooks,
    IReadOnlyList<SettingsTranslationDto> Translations, IReadOnlyList<SettingsIntegrationLogDto> IntegrationLogs,
    IReadOnlyList<SettingsBackupDto> Backups);

public sealed record SaveSettingsSectionDto(IReadOnlyDictionary<string, string> Values);
public sealed record SaveSettingsDeliveryZoneDto(string Name, string Governorate, string? Cities, decimal BaseFee,
    decimal FeePerKg, decimal FeePerKm, int EstimatedDays, bool IsActive);
public sealed record SaveSettingsBankAccountDto(string BankName, string AccountName, string AccountNumber,
    string? Iban, string Currency, bool IsPrimary, bool IsActive);
public sealed record CreateSettingsApiKeyDto(string Name, IReadOnlyList<string> Scopes, DateTime? ExpiresAt);
public sealed record SaveSettingsWebhookDto(string Event, string Url, bool IsActive);
public sealed record SaveSettingsTranslationDto(Guid? Id, string Key, string Arabic, string English, string? French);
