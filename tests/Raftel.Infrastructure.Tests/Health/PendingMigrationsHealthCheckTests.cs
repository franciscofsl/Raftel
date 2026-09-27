using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using NSubstitute;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class PendingMigrationsHealthCheckTests
{
    private readonly IDatabaseProbe _databaseProbe = Substitute.For<IDatabaseProbe>();
    private readonly PendingMigrationsHealthCheck _check;

    public PendingMigrationsHealthCheckTests()
    {
        _check = new PendingMigrationsHealthCheck(_databaseProbe);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportHealthy_WhenNoMigrationIsPending()
    {
        _databaseProbe.GetPendingMigrationsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        var result = await _check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WhenAtLeastOneMigrationIsPending()
    {
        _databaseProbe.GetPendingMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new[] { "20260101_Init" });

        var result = await _check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WithoutThrowing_WhenProbeFails()
    {
        _databaseProbe.GetPendingMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyCollection<string>>>(_ => throw new InvalidOperationException("boom"));

        var result = await _check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeNull();
    }
}
