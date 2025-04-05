using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Application.DomainEvents;
using Raftel.Core.BaseTypes;

namespace Raftel.Infrastructure.DomainEvents;

public class DefaultDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod("HandleAsync")!;
                await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
            }
        }
    }
}
 