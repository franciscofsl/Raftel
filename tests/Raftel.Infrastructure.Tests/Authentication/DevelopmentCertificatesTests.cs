using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OpenIddict.Server;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Authentication;

public class DevelopmentCertificatesTests
{
    private const string FakeConnectionString =
        "Data Source=(local);Initial Catalog=Test;Integrated Security=true";

    [Fact]
    public void AddRaftelData_WithDevelopmentEnvironment_ShouldRegisterDevelopmentCertificates()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();
        var environment = new FakeHostEnvironment(Environments.Development);

        services.AddRaftelData<TestingRaftelDbContext>(configuration, environment, "Default");

        var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddictServerOptions>>()
            .CurrentValue;

        options.EncryptionCredentials.Count.ShouldBeGreaterThan(0);
        options.SigningCredentials.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddRaftelData_WithNonDevelopmentEnvironment_ShouldNotRegisterDevelopmentCertificates()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();
        var environment = new FakeHostEnvironment(Environments.Production);

        services.AddRaftelData<TestingRaftelDbContext>(configuration, environment, "Default");

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddictServerOptions>>();

        // OpenIddict enforces that at least one encryption key must be registered.
        // In non-development environments, development certificates are not added,
        // so accessing the options will throw until real X.509 certificates are configured.
        Should.Throw<InvalidOperationException>(() => _ = optionsMonitor.CurrentValue);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = FakeConnectionString,
                ["Database:Provider"] = DatabaseProvider.SqlServer.ToString()
            })
            .Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Raftel.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
