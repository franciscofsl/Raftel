using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raftel.Application;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Outbox;

namespace Raftel.Infrastructure.Outbox;

/// <summary>Runs one processing cycle: claim a batch, deliver each message, record each outcome.</summary>
internal interface IOutboxBatchRunner
{
    /// <returns>The number of messages claimed; a full batch means there may be more waiting.</returns>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}

internal sealed class OutboxBatchRunner(
    IServiceScopeFactory scopeFactory,
    OutboxMessageDispatcher dispatcher,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxBatchRunner> logger) : IOutboxBatchRunner
{
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var claimed = await store.ClaimAsync(cancellationToken);

        foreach (var message in claimed)
        {
            await ProcessAsync(store, message, cancellationToken);
        }

        return claimed.Count;
    }

    private async Task ProcessAsync(IOutboxStore store, OutboxMessage message, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || LeaseIsNearlySpent(message))
        {
            await ReleaseAsync(store, message);
            return;
        }

        DeliveryOutcome outcome;

        try
        {
            outcome = await dispatcher.DeliverAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await ReleaseAsync(store, message);
            return;
        }

        var now = timeProvider.GetUtcNow();

        if (outcome.Succeeded)
        {
            message.MarkProcessed(now);
            LogDelivered(message);
        }
        else
        {
            var delay = OutboxRetrySchedule.DelayFor(message.AttemptCount, options.BaseRetryDelay, options.MaxRetryDelay);
            message.MarkFailed(now, outcome.Error, now + delay);
            LogFailed(message, outcome);
        }

        await SaveAsync(store, message);
    }

    private bool LeaseIsNearlySpent(OutboxMessage message) =>
        message.LockedUntil is { } lockedUntil
        && lockedUntil - timeProvider.GetUtcNow() < options.LockDuration / 10;

    private async Task ReleaseAsync(IOutboxStore store, OutboxMessage message)
    {
        message.Release();
        await SaveAsync(store, message);
    }

    private async Task SaveAsync(IOutboxStore store, OutboxMessage message)
    {
        if (!await store.SaveAsync(CancellationToken.None))
        {
            logger.LogWarning(
                "Outbox message {OutboxMessageId} lost its lease before its outcome was recorded; another processor now owns it.",
                message.Id);
        }
    }

    private void LogDelivered(OutboxMessage message) =>
        logger.LogInformation(
            "Outbox message {OutboxMessageId} of type {EventType} delivered on attempt {Attempt} with outcome {Outcome} (tenant {TenantId}, correlation {CorrelationId})",
            message.Id, message.Type, message.AttemptCount, "Delivered", message.TenantId, message.CorrelationId);

    private void LogFailed(OutboxMessage message, DeliveryOutcome outcome)
    {
        if (message.IsDeadLetter(options.MaxAttempts))
        {
            logger.LogError(outcome.Exception,
                "Outbox message {OutboxMessageId} of type {EventType} became a dead letter after {Attempt} attempts with outcome {Outcome} (tenant {TenantId}, correlation {CorrelationId}): {LastError}",
                message.Id, message.Type, message.AttemptCount, "DeadLettered", message.TenantId,
                message.CorrelationId, outcome.Error);
            return;
        }

        logger.LogWarning(outcome.Exception,
            "Outbox message {OutboxMessageId} of type {EventType} failed on attempt {Attempt} with outcome {Outcome} (tenant {TenantId}, correlation {CorrelationId}): {LastError}",
            message.Id, message.Type, message.AttemptCount, "Failed", message.TenantId, message.CorrelationId,
            outcome.Error);
    }
}
