using Microsoft.Extensions.DependencyInjection;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.DomainEvents;

/// <summary>
/// Resolves the <see cref="IDomainEventHandler{TEvent}"/> implementations registered for each
/// domain event's concrete type and invokes them. Events without a handler are a no-op.
/// </summary>
public class DomainEventsDispatcher(IServiceProvider serviceProvider) : IDomainEventsDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            await DispatchSingleAsync(domainEvent, cancellationToken);
        }
    }

    private async Task DispatchSingleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        var handleAsync = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        foreach (var handler in handlers)
        {
            await (Task)handleAsync.Invoke(handler, [domainEvent, cancellationToken])!;
        }
    }
}
