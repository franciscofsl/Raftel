using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Demo.Domain.Pirates.Events;

namespace Raftel.Demo.Application.Pirates.Events;

public sealed class PirateCrownedKingHandler : IDomainEventHandler<PirateCrownedKing>
{
    public Task HandleAsync(PirateCrownedKing domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Pirate {domainEvent.PirateId} has been crowned King of the Pirates.");
        return Task.CompletedTask;
    }
}
