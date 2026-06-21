using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.DomainEvents;

/// <summary>
/// Handles side effects triggered by a domain event of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
