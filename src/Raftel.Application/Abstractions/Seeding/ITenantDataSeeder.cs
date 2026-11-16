using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Application.Abstractions.Seeding;

/// <summary>
/// A seeder that runs once per tenant, with the current tenant set to the tenant being seeded.
/// </summary>
public interface ITenantDataSeeder : IDataSeeder
{
    /// <summary>
    /// Seeds the data of one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant being seeded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant seeders do not take part in the global pass.
    /// </summary>
    Task<Result> IDataSeeder.SeedAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
