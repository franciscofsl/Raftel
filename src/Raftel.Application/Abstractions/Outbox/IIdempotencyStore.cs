using Raftel.Domain.Outbox.ValueObjects;

namespace Raftel.Application.Abstractions.Outbox;

/// <summary>
/// Records which handlers have completed for which outbox message, so a redelivered message does not
/// re-run a handler that already finished.
/// </summary>
/// <remarks>
/// <see cref="MarkCompletedAsync"/> writes through the current unit of work, so the receipt commits or
/// rolls back together with the handler's own database writes.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>Gets whether <paramref name="handlerName"/> already completed for the message.</summary>
    Task<bool> HasCompletedAsync(OutboxMessageId messageId, string handlerName,
        CancellationToken cancellationToken = default);

    /// <summary>Records that <paramref name="handlerName"/> completed for the message and saves.</summary>
    Task MarkCompletedAsync(OutboxMessageId messageId, string handlerName,
        CancellationToken cancellationToken = default);
}
