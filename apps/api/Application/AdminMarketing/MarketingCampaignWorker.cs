namespace Mohandseto.Api.Application.AdminMarketing;

public sealed class MarketingCampaignWorker(IServiceScopeFactory scopeFactory, ILogger<MarketingCampaignWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sent = await scope.ServiceProvider.GetRequiredService<AdminMarketingService>().ProcessDueAsync(stoppingToken);
                if (sent > 0) logger.LogInformation("Delivered {Count} scheduled marketing campaigns", sent);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Failed to process scheduled marketing campaigns"); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}

