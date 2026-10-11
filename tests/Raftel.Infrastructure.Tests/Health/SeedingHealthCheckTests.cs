using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Infrastructure.Health;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Tests.Health;

public class SeedingHealthCheckTests
{
    [Theory]
    [InlineData(SeedingStatus.NotRequested, HealthStatus.Healthy)]
    [InlineData(SeedingStatus.Running, HealthStatus.Unhealthy)]
    [InlineData(SeedingStatus.Completed, HealthStatus.Healthy)]
    [InlineData(SeedingStatus.CompletedWithFailures, HealthStatus.Degraded)]
    [InlineData(SeedingStatus.Failed, HealthStatus.Unhealthy)]
    public async Task CheckHealthAsync_ShouldMapEachSeedingStatus(SeedingStatus status, HealthStatus expected)
    {
        var state = new SeedingState();
        Apply(state, status);

        var result = await new SeedingHealthCheck(state).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(expected);
    }

    private static void Apply(SeedingState state, SeedingStatus status)
    {
        switch (status)
        {
            case SeedingStatus.Running: state.MarkRunning(); break;
            case SeedingStatus.Completed: state.MarkCompleted(); break;
            case SeedingStatus.CompletedWithFailures: state.MarkCompletedWithFailures(); break;
            case SeedingStatus.Failed: state.MarkFailed(); break;
        }
    }
}
