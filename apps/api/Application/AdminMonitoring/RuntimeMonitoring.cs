using System.Collections.Concurrent;
using System.Diagnostics;
using System.Data.Common;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminMonitoring;

public sealed record RuntimeRequestSample(DateTime At, double DurationMs, int StatusCode);
public sealed record RuntimeDatabaseSample(DateTime At, double DurationMs, bool Succeeded);

public sealed class RuntimeMetrics
{
    private readonly ConcurrentQueue<RuntimeRequestSample> _requests = new();
    private readonly ConcurrentQueue<RuntimeDatabaseSample> _database = new();
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public void Record(double durationMs, int statusCode)
    {
        var now = DateTime.UtcNow;
        _requests.Enqueue(new(now, durationMs, statusCode));
        while (_requests.TryPeek(out var old) && old.At < now.AddHours(-24)) _requests.TryDequeue(out _);
    }

    public IReadOnlyList<RuntimeRequestSample> Snapshot() => _requests.ToArray();
    public IReadOnlyList<RuntimeDatabaseSample> DatabaseSnapshot() => _database.ToArray();
    public void RecordDatabase(TimeSpan duration, bool succeeded)
    {
        var now = DateTime.UtcNow;
        _database.Enqueue(new(now, duration.TotalMilliseconds, succeeded));
        while (_database.TryPeek(out var old) && old.At < now.AddHours(-24)) _database.TryDequeue(out _);
    }
}

/// <summary>Captures actual EF command latency without logging SQL text or parameter values.</summary>
public sealed class DatabaseMetricsInterceptor(RuntimeMetrics metrics) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result) { metrics.RecordDatabase(eventData.Duration, true); return result; }
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken ct = default) { metrics.RecordDatabase(eventData.Duration, true); return ValueTask.FromResult(result); }
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result) { metrics.RecordDatabase(eventData.Duration, true); return result; }
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken ct = default) { metrics.RecordDatabase(eventData.Duration, true); return ValueTask.FromResult(result); }
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result) { metrics.RecordDatabase(eventData.Duration, true); return result; }
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken ct = default) { metrics.RecordDatabase(eventData.Duration, true); return ValueTask.FromResult(result); }
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) => metrics.RecordDatabase(eventData.Duration, false);
    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken ct = default) { metrics.RecordDatabase(eventData.Duration, false); return Task.CompletedTask; }
}

public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RuntimeMetrics metrics)
    {
        var started = Stopwatch.GetTimestamp();
        try { await next(context); }
        finally { metrics.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.Response.StatusCode); }
    }
}

public sealed class BlockedIpMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(ip))
        {
            var now = DateTime.UtcNow;
            var blocked = await db.BlockedIpAddresses.AsNoTracking().AnyAsync(
                x => x.IsActive && x.IpAddress == ip && (x.ExpiresAt == null || x.ExpiresAt > now),
                context.RequestAborted);
            if (blocked)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "about:blank",
                    title = "تم حظر عنوان الشبكة لهذا الطلب",
                    status = StatusCodes.Status403Forbidden,
                    code = "IP_BLOCKED",
                    traceId = context.TraceIdentifier,
                }, context.RequestAborted);
                return;
            }
        }
        await next(context);
    }
}

public sealed class SystemErrorCaptureMiddleware(RequestDelegate next, IServiceScopeFactory scopes, ILogger<SystemErrorCaptureMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            if (exception is Mohandseto.Api.Application.Common.ApiException) throw;
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                    $"{exception.GetType().FullName}|{exception.Message}|{context.Request.Path}")))[..24];
                var existing = await db.SystemErrorEvents.FirstOrDefaultAsync(
                    x => x.Fingerprint == fingerprint && x.ResolvedAt == null, context.RequestAborted);
                if (existing is null)
                {
                    var id = Guid.NewGuid();
                    db.SystemErrorEvents.Add(new SystemErrorEvent
                    {
                        Id = id,
                        Fingerprint = fingerprint,
                        Number = $"ERR-{id.ToString("N")[..8].ToUpperInvariant()}",
                        Severity = SystemErrorSeverity.Error,
                        Service = "API",
                        Message = Sanitize(exception.Message, 1000) ?? "Unhandled exception",
                        ExceptionType = exception.GetType().FullName,
                        StackTrace = Sanitize(exception.StackTrace, 8000),
                        ContextJson = JsonSerializer.Serialize(new { method = context.Request.Method, path = context.Request.Path.Value }),
                        CorrelationId = context.TraceIdentifier,
                        Path = context.Request.Path.Value,
                        UserId = ParseGuid(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")),
                        TenantId = ParseGuid(context.User.FindFirstValue("tenant_id")),
                    });
                }
                else
                {
                    existing.OccurrenceCount++;
                    existing.LastOccurredAt = DateTime.UtcNow;
                    existing.CorrelationId = context.TraceIdentifier;
                }
                await db.SaveChangesAsync(context.RequestAborted);
            }
            catch (Exception captureError) { logger.LogError(captureError, "Failed to persist system error event"); }
            throw;
        }
    }

    private static Guid? ParseGuid(string? raw) => Guid.TryParse(raw, out var id) ? id : null;
    private static string? Sanitize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Replace("\0", "");
        return cleaned[..Math.Min(cleaned.Length, max)];
    }
}
