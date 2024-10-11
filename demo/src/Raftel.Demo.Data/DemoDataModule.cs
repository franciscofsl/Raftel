using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Data;
using Raftel.Shared.Modules;

namespace Raftel.Demo.Data;

[ExcludeFromCodeCoverage]
[ModulesToInclude(typeof(EfCoreModule))]
public class DemoDataModule : RaftelModule
{
    public override void ConfigureCustomServices(IServiceCollection services)
    {
        services.AddRaftelDbContext<DemoDbContext>();
    }
}