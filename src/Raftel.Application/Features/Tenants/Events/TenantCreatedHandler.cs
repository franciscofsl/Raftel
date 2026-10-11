using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Features.Tenants;

namespace Raftel.Application.Features.Tenants.Events;

/// <summary>
/// Runs the tenant seeders for a tenant that was just created. A failure aborts the transaction the
/// tenant was created in, so a tenant is never left half-seeded.
/// </summary>
internal sealed class TenantCreatedHandler(ISeedRunner seedRunner) : IDomainEventHandler<TenantCreated>
{
    public async Task HandleAsync(TenantCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var result = await seedRunner.RunForTenantAsync(domainEvent.TenantId, cancellationToken);
        if (result.IsFailure)
        {
            throw new SeedingFailedException(result.Error);
        }
    }
}
