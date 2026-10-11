using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Health;

public sealed class SeedingHealthCheck(SeedingState seedingState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = seedingState.Status switch
        {
            SeedingStatus.Running => HealthCheckResult.Unhealthy("Startup seeding is in progress."),
            SeedingStatus.Failed => HealthCheckResult.Unhealthy("Startup seeding failed."),
            SeedingStatus.CompletedWithFailures => HealthCheckResult.Degraded("Startup seeding completed with failures."),
            _ => HealthCheckResult.Healthy()
        };

        return Task.FromResult(result);
    }
}
