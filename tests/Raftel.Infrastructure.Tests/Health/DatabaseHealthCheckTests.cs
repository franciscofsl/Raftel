using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class DatabaseHealthCheckTests
{
    private readonly IDatabaseProbe _databaseProbe = Substitute.For<IDatabaseProbe>();

    private static DatabaseHealthCheck CreateCheck(IDatabaseProbe probe, HealthOptions options = null)
    {
        return new DatabaseHealthCheck(probe, TimeProvider.System, Options.Create(options ?? new HealthOptions()));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportHealthy_WhenConnectionSucceedsQuickly()
    {
        _databaseProbe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(true);
        var check = CreateCheck(_databaseProbe, new HealthOptions { DatabaseSlowThreshold = TimeSpan.FromDays(1) });

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportDegraded_WhenConnectionExceedsSlowThreshold()
    {
        _databaseProbe.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(20);
                return true;
            });
        var check = CreateCheck(_databaseProbe, new HealthOptions { DatabaseSlowThreshold = TimeSpan.Zero });

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WhenConnectionFails()
    {
        _databaseProbe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(false);
        var check = CreateCheck(_databaseProbe);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WhenProbeThrows_WithoutPropagatingException()
    {
        _databaseProbe.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("connection string=super-secret;"));
        var check = CreateCheck(_databaseProbe);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_UnhealthyResult_ShouldNotContainExceptionMessageOrConnectionString()
    {
        const string secret = "Server=myserver;Password=super-secret;";
        _databaseProbe.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException(secret));
        var check = CreateCheck(_databaseProbe);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Exception.ShouldBeNull();
        result.Description.ShouldNotBeNull();
        result.Description.ShouldNotContain(secret);
        result.Description.ShouldNotContain("Password");
        (result.Data is null || !result.Data.Values.Any(v => v?.ToString()?.Contains(secret) == true)).ShouldBeTrue();
    }
}
