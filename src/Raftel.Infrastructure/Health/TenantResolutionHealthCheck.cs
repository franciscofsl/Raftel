using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;

namespace Raftel.Infrastructure.Health;

public sealed class TenantResolutionHealthCheck(ITenantsRepository tenantsRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var page = PageRequest.Create(1, 1).Value;
            await tenantsRepository.ListPagedAsync(page, cancellationToken: cancellationToken);
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Tenant store did not respond.");
        }

        return HealthCheckResult.Healthy();
    }
}
