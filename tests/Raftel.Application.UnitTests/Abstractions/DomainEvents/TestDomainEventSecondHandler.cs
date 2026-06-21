using Raftel.Application.Abstractions.DomainEvents;

namespace Raftel.Application.UnitTests.Abstractions.DomainEvents;

public class TestDomainEventSecondHandler(ISpy spy) : IDomainEventHandler<TestDomainEvent>
{
    public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        spy.Intercept($"Handler2: {domainEvent.Message}");
        return Task.CompletedTask;
    }
}
