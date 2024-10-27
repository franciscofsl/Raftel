using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Data;
using Raftel.Demo.Core.Samples;
using Raftel.Demo.Data.Repositories.Samples;
using Raftel.Shared.Modules;

namespace Raftel.Demo.Data;

[ExcludeFromCodeCoverage]
public class DemoDataModule : EfCoreModule
{
    public override void ConfigureCustomServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureCustomServices(services, configuration);
        services.AddRaftelDbContext<DemoDbContext>();
        services.AddTransient<ISamplesRepository, SamplesRepository>();
    }
}