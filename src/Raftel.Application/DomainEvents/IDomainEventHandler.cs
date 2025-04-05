using Raftel.Core.BaseTypes;

namespace Raftel.Application.DomainEvents;

public interface IDomainEventHandler<TDomainEvent> where TDomainEvent : IDomainEvent
{
    public Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}