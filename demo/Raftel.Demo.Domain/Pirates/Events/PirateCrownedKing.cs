using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates.Events;

public sealed record PirateCrownedKing(PirateId PirateId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
