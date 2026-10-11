using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Raftel.Infrastructure.Health;

public sealed class PendingMigrationsHealthCheck(IDatabaseProbe databaseProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> pendingMigrations;
        try
        {
            pendingMigrations = await databaseProbe.GetPendingMigrationsAsync(cancellationToken);
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Could not determine migration state.");
        }

        return pendingMigrations.Count > 0
            ? HealthCheckResult.Unhealthy($"{pendingMigrations.Count} pending migration(s).")
            : HealthCheckResult.Healthy();
    }
}
