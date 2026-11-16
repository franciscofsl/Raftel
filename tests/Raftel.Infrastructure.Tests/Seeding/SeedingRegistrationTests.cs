using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Demo.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Seeding;

public class SeedingRegistrationTests
{
    [Fact]
    public void AddRaftelData_ShouldResolveSeedRunnerAndBindSeedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=unused;Database=unused",
                ["Database:Provider"] = "SqlServer",
                ["Raftel:Seed:AdminRoleName"] = "Root",
                ["Raftel:Seed:FailFastOnError"] = "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRaftelData<TestingRaftelDbContext>(configuration, configureSeed: o => o.ApplyMigrationsOnStartup = true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISeedRunner>().ShouldNotBeNull();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        options.AdminRoleName.ShouldBe("Root");
        options.FailFastOnError.ShouldBeFalse();
        options.ApplyMigrationsOnStartup.ShouldBeTrue();
    }
}
