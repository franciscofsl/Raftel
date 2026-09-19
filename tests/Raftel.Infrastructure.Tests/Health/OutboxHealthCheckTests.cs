using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class OutboxHealthCheckTests
{
    private readonly IOutboxHealthProbe _outboxHealthProbe = Substitute.For<IOutboxHealthProbe>();
    private static readonly HealthOptions Options = new()
    {
        OutboxLagThreshold = TimeSpan.FromMinutes(5),
        DeadLetterThreshold = 10
    };

    private OutboxHealthCheck CreateCheck() => new(_outboxHealthProbe, Microsoft.Extensions.Options.Options.Create(Options));

    [Fact]
    public async Task CheckHealthAsync_ShouldReportHealthy_WhenUnderBothThresholds()
    {
        _outboxHealthProbe.GetOldestUnprocessedMessageAgeAsync(Arg.Any<CancellationToken>())
            .Returns(TimeSpan.FromMinutes(1));
        _outboxHealthProbe.GetDeadLetterCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportDegraded_WhenOldestUnprocessedMessageExceedsLagThreshold()
    {
        _outboxHealthProbe.GetOldestUnprocessedMessageAgeAsync(Arg.Any<CancellationToken>())
            .Returns(TimeSpan.FromMinutes(10));
        _outboxHealthProbe.GetDeadLetterCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WhenDeadLettersExceedThreshold()
    {
        _outboxHealthProbe.GetOldestUnprocessedMessageAgeAsync(Arg.Any<CancellationToken>())
            .Returns((TimeSpan?)null);
        _outboxHealthProbe.GetDeadLetterCountAsync(Arg.Any<CancellationToken>()).Returns(11);

        var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WithoutThrowing_WhenProbeFails()
    {
        _outboxHealthProbe.GetOldestUnprocessedMessageAgeAsync(Arg.Any<CancellationToken>())
            .Returns<Task<TimeSpan?>>(_ => throw new InvalidOperationException("boom"));

        var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeNull();
    }
}
