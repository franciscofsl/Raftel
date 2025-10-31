using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure;
using Raftel.Infrastructure.Data;
using DatabaseProvider = Raftel.Infrastructure.Data.DatabaseProvider;

[assembly: InternalsVisibleTo("Raftel.Application.UnitTests")]

namespace Raftel.Demo.Infrastructure;

public static class DependencyInjection
{
    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString)
    {
        AddSampleInfrastructure(services, connectionString, DatabaseProvider.SqlServer);
    }

    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString, DatabaseProvider databaseProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = connectionString,
                ["Database:Provider"] = databaseProvider.ToString()
            })
            .Build();
        services.AddRaftelData<TestingRaftelDbContext>(configuration, "TestConnection");
        services.AddScoped(typeof(IPirateRepository), typeof(PirateRepository));
        services.AddScoped(typeof(IShipRepository), typeof(ShipRepository));
    }
}