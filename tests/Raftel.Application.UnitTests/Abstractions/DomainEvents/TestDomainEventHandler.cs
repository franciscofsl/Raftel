using Raftel.Application.Abstractions.DomainEvents;

namespace Raftel.Application.UnitTests.Abstractions.DomainEvents;

public class TestDomainEventHandler(ISpy spy) : IDomainEventHandler<TestDomainEvent>
{
    public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        spy.Intercept($"Handler1: {domainEvent.Message}");
        return Task.CompletedTask;
    }
}
