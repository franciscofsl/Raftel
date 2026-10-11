using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raftel.Application;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Polls the outbox and delivers pending messages until the host shuts down. A cycle that fills the batch
/// is followed immediately by another; an infrastructure failure is logged and retried on the next poll
/// instead of ending the loop.
/// </summary>
internal sealed class OutboxProcessor(
    IOutboxBatchRunner runner,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollingInterval, timeProvider);

        try
        {
            do
            {
                await DrainAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int claimed;

            try
            {
                claimed = await runner.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox processing cycle failed; it will be retried on the next poll.");
                return;
            }

            if (claimed < options.BatchSize)
            {
                return;
            }
        }
    }
}
