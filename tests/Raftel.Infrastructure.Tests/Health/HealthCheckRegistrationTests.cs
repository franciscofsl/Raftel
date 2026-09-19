using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Infrastructure.Health;

namespace Raftel.Infrastructure.Tests.Health;

public class HealthCheckRegistrationTests
{
    private static IReadOnlyList<HealthCheckRegistration> GetRegistrations(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ToList();
    }

    [Fact]
    public void AddRaftelHealthChecks_ShouldRegisterOnlyMandatoryChecks_ByDefault()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());

        services.AddRaftelHealthChecks();

        var registrations = GetRegistrations(services);

        registrations.Select(r => r.Name).ShouldBe(
            [HealthCheckNames.Database, HealthCheckNames.Migrations, HealthCheckNames.Startup],
            ignoreOrder: true);
        registrations.ShouldAllBe(r => r.Tags.Contains(HealthCheckTags.Ready));
        registrations.ShouldAllBe(r => r.Timeout == TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void AddRaftelHealthChecks_ShouldRegisterTenantCheck_WhenOptedIn()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());

        services.AddRaftelHealthChecks(options => options.EnableTenantResolutionCheck = true);

        var registrations = GetRegistrations(services);

        registrations.Select(r => r.Name).ShouldContain(HealthCheckNames.Tenants);
    }

    [Fact]
    public void AddRaftelHealthChecks_ShouldRegisterOutboxCheck_WhenOptedInAndProbeRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());
        services.AddSingleton(Substitute.For<IOutboxHealthProbe>());

        services.AddRaftelHealthChecks(options => options.EnableOutboxCheck = true);

        var registrations = GetRegistrations(services);

        registrations.Select(r => r.Name).ShouldContain(HealthCheckNames.Outbox);
    }

    [Fact]
    public void AddRaftelHealthChecks_ShouldThrow_WhenOutboxCheckEnabledWithoutProbe()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());

        Should.Throw<InvalidOperationException>(
            () => services.AddRaftelHealthChecks(options => options.EnableOutboxCheck = true));
    }

    [Fact]
    public void AddRaftelHealthChecks_ShouldApplyConfiguredTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IDatabaseProbe>());

        services.AddRaftelHealthChecks(options => options.CheckTimeout = TimeSpan.FromSeconds(7));

        var registrations = GetRegistrations(services);

        registrations.ShouldAllBe(r => r.Timeout == TimeSpan.FromSeconds(7));
    }
}
