using NoticeSaaS.Application.Sync;
using NoticeSaaS.Infrastructure;

namespace NoticeSaaS.Workers;

public sealed class IncomeTaxSyncWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IncomeTaxSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Income Tax sync worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                var processor = scope.ServiceProvider.GetRequiredService<ISyncJobProcessor>();

                var enqueued = await syncService.EnqueueDueScheduledAsync(stoppingToken);
                if (enqueued > 0)
                {
                    logger.LogInformation("Enqueued {Count} due sync job(s).", enqueued);
                }

                await processor.ProcessPendingAsync(maxJobs: 10, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Sync worker iteration failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
