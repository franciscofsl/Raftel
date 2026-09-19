using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Options;

namespace Raftel.Infrastructure.Health;

public sealed class DatabaseHealthCheck(
    IDatabaseProbe databaseProbe,
    TimeProvider timeProvider,
    IOptions<HealthOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = timeProvider.GetTimestamp();

        bool canConnect;
        try
        {
            canConnect = await databaseProbe.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database connection failed.");
        }

        if (!canConnect)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.");
        }

        var elapsed = timeProvider.GetElapsedTime(startedAt);
        return elapsed > options.Value.DatabaseSlowThreshold
            ? HealthCheckResult.Degraded($"Database connection took {elapsed.TotalMilliseconds:F0}ms.")
            : HealthCheckResult.Healthy();
    }
}
