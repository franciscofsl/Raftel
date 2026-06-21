using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.DomainEvents;

/// <summary>
/// Dispatches domain events to their registered <see cref="IDomainEventHandler{TEvent}"/>.
/// </summary>
public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
