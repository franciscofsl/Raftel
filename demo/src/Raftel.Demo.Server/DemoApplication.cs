using Raftel.Demo.Application;
using Raftel.Demo.Data;
using Raftel.Demo.Infrastructure;
using Raftel.Shared.Modules;

namespace Raftel.Demo.Server;

[ModulesToInclude(typeof(DemoDataModule),
    typeof(DemoApplicationModule),
    typeof(DemoInfrastructureModule))]
public class DemoApplication : RaftelApplication
{
}