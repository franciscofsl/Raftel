using Raftel.Application.DomainEvents;

namespace Raftel.Infrastructure.Tests.DomainEvents;

public class SampleDomainEventHandler : IDomainEventHandler<SampleDomainEvent>
{
    public Task HandleAsync(SampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}