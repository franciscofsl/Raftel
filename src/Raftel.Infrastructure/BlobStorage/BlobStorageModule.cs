using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.BlobStorage.Contracts;
using Raftel.Shared.Modules;

namespace Raftel.Infrastructure.BlobStorage;

public class BlobStorageModule : RaftelModule
{
    public override void ConfigureCustomServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection("AzureBlobStorage"));
        services.AddSingleton<IAzureBlobStorageClient, AzureBlobStorageClient>();
    }
}