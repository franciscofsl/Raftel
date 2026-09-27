using Raftel.Application;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.DomainEvents;

/// <summary>
/// Handles side effects triggered by a domain event of type <typeparamref name="TEvent"/>.
/// </summary>
/// <remarks>
/// Dispatch happens from <c>SavedChanges</c>/<c>SavedChangesAsync</c>, after the <c>SaveChanges</c>
/// call that raised the event has flushed. If the command pipeline has <c>TransactionMiddleware</c>
/// registered, that <c>SaveChanges</c> call runs inside the command's still-open transaction, so any
/// database write this handler performs (via a repository, followed by its own
/// <see cref="IUnitOfWork.CommitAsync"/>) joins that same transaction: it is rolled back together
/// with the rest of the command if the command later fails, and persists only once the command's
/// transaction commits. Without <c>TransactionMiddleware</c> registered, such a write lands in its
/// own separate, already-committed transaction instead.
/// This guarantee covers database writes only. Non-database side effects performed here - sending
/// an email, calling an external HTTP API - are not transactional and are not rolled back if the
/// command subsequently fails.
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
