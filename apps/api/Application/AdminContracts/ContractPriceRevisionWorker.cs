namespace Mohandseto.Api.Application.AdminContracts;

/// <summary>Applies scheduled contract prices without requiring an administrator to open the dashboard.</summary>
public sealed class ContractPriceRevisionWorker(IServiceScopeFactory scopeFactory, ILogger<ContractPriceRevisionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var applied = await scope.ServiceProvider.GetRequiredService<AdminContractService>().ProcessDuePriceRevisionsAsync(stoppingToken);
                if (applied > 0) logger.LogInformation("Applied {Count} scheduled contract price revisions", applied);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Failed to apply scheduled contract price revisions"); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
