using Raftel.Application;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.DomainEvents;

/// <summary>
/// Handles side effects triggered by a domain event of type <typeparamref name="TEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>In-process mode</b> (outbox disabled, the default). Dispatch happens from
/// <c>SavedChanges</c>/<c>SavedChangesAsync</c>, after the <c>SaveChanges</c> call that raised the event
/// has flushed. If the command pipeline has <c>TransactionMiddleware</c> registered, that
/// <c>SaveChanges</c> call runs inside the command's still-open transaction, so any database write this
/// handler performs (via a repository, followed by its own <see cref="IUnitOfWork.CommitAsync"/>) joins
/// that same transaction: it is rolled back together with the rest of the command if the command later
/// fails, and persists only once the command's transaction commits. Without <c>TransactionMiddleware</c>
/// registered, such a write lands in its own separate, already-committed transaction instead. A handler
/// that throws fails the command.
/// </para>
/// <para>
/// <b>Outbox mode</b> (<see cref="OutboxOptions.Enabled"/>). The event is stored with the command's own
/// state change and delivered later by a background processor, so a handler failure never fails the
/// command. Each delivery runs in its own DI scope and its own transaction: the handler's database writes
/// and the record that it completed commit together. The tenant and correlation identifier of the
/// originating request are restored; there is no <c>HttpContext</c> and no current user. Delivery is
/// at-least-once and unordered, and failed deliveries are retried with backoff until they become dead
/// letters.
/// </para>
/// <para>
/// In either mode, only database writes are transactional. Non-database side effects performed here -
/// sending an email, calling an external HTTP API - are not rolled back if the command subsequently
/// fails, and in outbox mode they can run more than once for the same event, so they must be idempotent.
/// </para>
/// </remarks>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
