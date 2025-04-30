using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure;

[assembly: InternalsVisibleTo("Raftel.Application.UnitTests")]

namespace Raftel.Demo.Infrastructure;

public static class DependencyInjection
{
    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = connectionString
            })
            .Build();
        services.AddRaftelData<TestingRaftelDbContext>(configuration, "TestConnection");
        services.AddScoped(typeof(IPirateRepository), typeof(PirateRepository));
    }
}