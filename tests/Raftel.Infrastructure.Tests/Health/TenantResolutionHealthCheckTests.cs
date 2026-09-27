using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raftel.Application.Abstractions.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class TenantResolutionHealthCheckTests
{
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly TenantResolutionHealthCheck _check;

    public TenantResolutionHealthCheckTests()
    {
        _check = new TenantResolutionHealthCheck(_tenantsRepository);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportHealthy_WhenTenantStoreResponds()
    {
        var result = await _check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReportUnhealthy_WithoutThrowing_WhenTenantStoreFails()
    {
        _tenantsRepository
            .ListPagedAsync(Arg.Any<PageRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeNull();
    }
}
