using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raftel.Application;
using Raftel.Application.Abstractions.Outbox;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Deletes processed outbox messages, and their handler receipts, once they are older than the retention
/// period. Pending messages and dead letters are never deleted: the first are still owed delivery, the
/// second are evidence somebody needs to look at.
/// </summary>
internal sealed class OutboxCleanupService(
    IServiceScopeFactory scopeFactory,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxCleanupService> logger) : BackgroundService
{
    internal static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        try
        {
            do
            {
                await RunOnceSafelyAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <returns>The number of messages deleted.</returns>
    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var cutoff = timeProvider.GetUtcNow() - options.RetentionPeriod;
        var deleted = await store.DeleteProcessedBeforeAsync(cutoff, cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("Outbox cleanup deleted {DeletedCount} messages processed before {Cutoff}.",
                deleted, cutoff);
        }

        return deleted;
    }

    private async Task RunOnceSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Outbox cleanup failed; it will be retried on the next run.");
        }
    }
}
