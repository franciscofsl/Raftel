using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Raftel.Infrastructure.Health;

public sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public void MarkReady()
    {
        _isReady = true;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _isReady
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Startup has not completed yet.");

        return Task.FromResult(result);
    }
}
