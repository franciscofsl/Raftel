using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Application.Abstractions.Seeding;

/// <summary>
/// Executes the registered seeders.
/// </summary>
public interface ISeedRunner
{
    /// <summary>
    /// Runs every global seeder in order, then every tenant seeder once per existing tenant.
    /// Each execution runs in its own scope and transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs every tenant seeder for one tenant in the ambient scope and transaction, so a failure
    /// rolls back together with the operation that created the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to seed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result> RunForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
