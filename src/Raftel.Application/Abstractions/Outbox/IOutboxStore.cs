using Raftel.Domain.Outbox;

namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// Persistence for outbox messages on the processing side. Queries ignore the current tenant: the
/// processor serves every tenant.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Atomically leases up to the configured batch size of deliverable messages, oldest first, counting
    /// one attempt on each. A message is deliverable when it is unprocessed, has attempts left, is due
    /// for its next attempt and is not currently leased by another processor.
    /// </summary>
    /// <returns>The claimed messages, tracked so that <see cref="SaveAsync"/> persists their outcome.</returns>
    Task<IReadOnlyList<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the outcome applied to messages returned by <see cref="ClaimAsync"/>. The write only
    /// succeeds while this processor still holds the lease: if the lease expired and another processor
    /// claimed the message meanwhile, the outcome is discarded so it cannot overwrite theirs.
    /// </summary>
    /// <returns><see langword="false"/> when the lease was lost and the outcome was discarded.</returns>
    Task<bool> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the dead letters: unprocessed messages that used all their attempts.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetDeadLettersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes messages processed before <paramref name="cutoff"/>, with their handler receipts.
    /// Unprocessed messages and dead letters are never deleted.
    /// </summary>
    /// <returns>The number of deleted messages.</returns>
    Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
