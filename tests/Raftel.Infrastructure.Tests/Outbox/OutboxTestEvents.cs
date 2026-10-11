using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Domain.Abstractions;

namespace Raftel.Infrastructure.Tests.Outbox;

/// <summary>An event whose own state refers back to itself, so it cannot be serialized.</summary>
public sealed record CyclicTestEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public CyclicTestEvent Self => this;
}

public sealed class CyclicTestEventHandler : IDomainEventHandler<CyclicTestEvent>
{
    public Task HandleAsync(CyclicTestEvent domainEvent, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

/// <summary>An event type that no handler is registered for.</summary>
public sealed record UnhandledTestEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
