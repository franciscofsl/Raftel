using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Abstractions.DomainEvents;

public sealed record TestDomainEvent(string Message) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
