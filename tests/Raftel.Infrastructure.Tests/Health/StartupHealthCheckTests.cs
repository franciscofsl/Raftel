using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class StartupHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_BeforeMarkedReady()
    {
        var check = new StartupHealthCheck();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportHealthy_AfterMarkedReady()
    {
        var check = new StartupHealthCheck();
        check.MarkReady();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldRemainUnhealthy_WhenNeverMarkedReady_SimulatingFailedStartup()
    {
        var check = new StartupHealthCheck();

        var first = await check.CheckHealthAsync(new HealthCheckContext());
        var second = await check.CheckHealthAsync(new HealthCheckContext());

        first.Status.ShouldBe(HealthStatus.Unhealthy);
        second.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
