namespace Mohandseto.Api.Application.AdminContent;

/// <summary>Delivers due administrative campaigns without requiring a dashboard session.</summary>
public sealed class ContentDispatchWorker(IServiceScopeFactory scopeFactory, ILogger<ContentDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var delivered = await scope.ServiceProvider.GetRequiredService<AdminContentService>().ProcessDueAsync(stoppingToken);
                if (delivered > 0) logger.LogInformation("Delivered {Count} scheduled content dispatches", delivered);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Failed to process scheduled content dispatches"); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
