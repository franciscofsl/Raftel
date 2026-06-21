using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Tests.BaseTypes;

public sealed record CustomerRenamed(CustomerId CustomerId, string NewName) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
