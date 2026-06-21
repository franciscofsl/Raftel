using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        AddSampleInfrastructure(services, connectionString, DatabaseProvider.SqlServer, CreateDevelopmentEnvironment());
    }

    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString, DatabaseProvider databaseProvider)
    {
        AddSampleInfrastructure(services, connectionString, databaseProvider, CreateDevelopmentEnvironment());
    }

    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString, IHostEnvironment environment)
    {
        AddSampleInfrastructure(services, connectionString, DatabaseProvider.SqlServer, environment);
    }

    public static void AddSampleInfrastructure(this IServiceCollection services,
        string connectionString, DatabaseProvider databaseProvider, IHostEnvironment environment)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = connectionString,
                ["Database:Provider"] = databaseProvider.ToString()
            })
            .Build();
        services.AddRaftelData<TestingRaftelDbContext>(configuration, environment, "TestConnection");
        services.AddScoped(typeof(IPirateRepository), typeof(PirateRepository));
        services.AddScoped(typeof(IShipRepository), typeof(ShipRepository));
    }

    private static IHostEnvironment CreateDevelopmentEnvironment()
    {
        return new DevelopmentHostEnvironment();
    }

    private sealed class DevelopmentHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Raftel";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}