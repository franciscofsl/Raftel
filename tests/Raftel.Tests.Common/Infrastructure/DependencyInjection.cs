using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Domain.Validators;
using Raftel.Infrastructure;
using Raftel.Tests.Common.Domain;
using Raftel.Tests.Common.Infrastructure.Data;

[assembly: InternalsVisibleTo("Raftel.Application.UnitTests")]

namespace Raftel.Tests.Common.Infrastructure;

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