using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Raftel.Application.Abstractions.Health;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class HealthCheckTimeoutIsolationTests
{
    private sealed class NeverCompletingHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return HealthCheckResult.Healthy();
        }
    }

    [Fact]
    public async Task HealthCheckService_ShouldReportTimedOutCheckAsUnhealthy_AndStillReportOtherChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());

        services.AddRaftelHealthChecks(options => options.CheckTimeout = TimeSpan.FromMilliseconds(50));
        services.AddHealthChecks().AddCheck<NeverCompletingHealthCheck>("hung", timeout: TimeSpan.FromMilliseconds(50));

        using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        report.Entries["hung"].Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries[HealthCheckNames.Startup].Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries.ShouldContainKey(HealthCheckNames.Database);
        report.Entries.ShouldContainKey(HealthCheckNames.Migrations);
    }
}
