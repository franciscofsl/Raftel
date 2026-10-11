using Microsoft.Extensions.Hosting;
using Raftel.Demo.Infrastructure;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Tests.Outbox;

public class OutboxRegistrationTests
{
    private const string ConnectionString = "Server=localhost;Database=unused";

    [Theory]
    [InlineData(nameof(OutboxOptions.BatchSize))]
    [InlineData(nameof(OutboxOptions.MaxAttempts))]
    [InlineData(nameof(OutboxOptions.LockDuration))]
    public void EnablingWithAnInvalidSetting_ShouldFailAtRegistration_NamingTheSetting(string setting)
    {
        var services = new ServiceCollection();

        var exception = Should.Throw<InvalidOperationException>(() =>
            services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer, options =>
            {
                options.Enabled = true;
                switch (setting)
                {
                    case nameof(OutboxOptions.BatchSize): options.BatchSize = 0; break;
                    case nameof(OutboxOptions.MaxAttempts): options.MaxAttempts = -3; break;
                    case nameof(OutboxOptions.LockDuration): options.LockDuration = TimeSpan.Zero; break;
                }
            }));

        exception.Message.ShouldContain(setting);
    }

    [Fact]
    public void InvalidSettings_ShouldBeIgnored_WhileTheOutboxIsDisabled()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() =>
            services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer,
                options => options.BatchSize = 0));
    }

    [Fact]
    public void EnablingWithoutRegisteringHandlers_ShouldFailWhenTheHostBuildsItsHostedServices()
    {
        var services = new ServiceCollection();
        services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer, o => o.Enabled = true);
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<InvalidOperationException>(() => provider.GetServices<IHostedService>().ToList());

        exception.Message.ShouldContain("AddRaftelApplication");
    }

    [Fact]
    public void EnablingAfterRegisteringHandlers_ShouldBuildTheHostedServices()
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Raftel.Demo.Application.Pirates.CreatePirate.CreatePirateCommand).Assembly));
        services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer, o => o.Enabled = true);
        using var provider = services.BuildServiceProvider();

        Should.NotThrow(() => provider.GetServices<IHostedService>().ToList());
    }

    [Fact]
    public void EnablingTheOutbox_ShouldRegisterTheProcessor()
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Raftel.Demo.Application.Pirates.CreatePirate.CreatePirateCommand).Assembly));
        services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer, o => o.Enabled = true);
        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        hostedServices.ShouldContain(service => service is OutboxProcessor);
        hostedServices.ShouldContain(service => service is OutboxCleanupService);
    }

    [Fact]
    public void DisablingTheOutbox_ShouldRegisterNoHostedServices()
    {
        var services = new ServiceCollection();
        services.AddSampleInfrastructure(ConnectionString, DatabaseProvider.SqlServer);
        using var provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>()
            .Where(service => service.GetType().Namespace == "Raftel.Infrastructure.Outbox")
            .ShouldBeEmpty();
    }
}
