namespace Mohandseto.Api.Application.AdminIntegrations;

public sealed record IntegrationFieldDto(string Code, string LabelAr, string Type, bool Required, bool Sensitive,
    IReadOnlyList<string> Options, string? Help = null);
public sealed record IntegrationCardDto(string Code, string NameAr, string NameEn, string Provider, string Icon,
    string Tone, string DescriptionAr, bool IsConnected, bool IsEnabled, string Status, DateTime? LastHealthCheckAt,
    DateTime? LastSuccessfulSyncAt, int OperationsToday, decimal SuccessRate);
public sealed record IntegrationSummaryDto(int Total, int Connected, int Disconnected, int FailedToday, int Retryable);
public sealed record IntegrationDashboardDto(IntegrationSummaryDto Summary, IReadOnlyList<IntegrationCardDto> Integrations,
    IReadOnlyList<IntegrationOperationDto> RecentOperations);
public sealed record IntegrationMetricDto(string LabelAr, string Value, string? Hint = null);
public sealed record IntegrationDetailDto(IntegrationCardDto Integration, IReadOnlyList<IntegrationFieldDto> Fields,
    IReadOnlyDictionary<string, string> Values, IReadOnlyList<IntegrationMetricDto> Metrics,
    IReadOnlyList<IntegrationOperationDto> RecentOperations);
public sealed record IntegrationOperationDto(Guid Id, string Number, string IntegrationCode, string IntegrationName,
    string Operation, string? Reference, string Status, int Attempt, int MaxAttempts, int DurationMs, string? Error,
    string? ErrorCode, string? Endpoint, bool IsRetryable, DateTime? NextRetryAt, DateTime? ResolvedAt,
    DateTime StartedAt, DateTime? CompletedAt);
public sealed record IntegrationOperationsPageDto(int Total, int Page, int PageSize, int Retryable,
    IReadOnlyList<IntegrationOperationDto> Items);
public sealed record SaveIntegrationConnectionDto(IReadOnlyDictionary<string, string> Values);
public sealed record RunIntegrationOperationDto(string? Operation = null, string? Reference = null);
public sealed record IntegrationActionResultDto(bool Succeeded, string MessageAr, IntegrationCardDto? Integration,
    IntegrationOperationDto Operation);
