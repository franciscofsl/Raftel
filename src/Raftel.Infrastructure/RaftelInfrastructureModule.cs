using Raftel.Infrastructure.BlobStorage;
using Raftel.Shared.Modules;

namespace Raftel.Infrastructure;

[ModulesToInclude(typeof(OutboxModule),
    typeof(BlobStorageModule))]
public class RaftelInfrastructureModule : RaftelModule
{
    
}