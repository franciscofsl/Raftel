using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Data;

namespace Raftel.Inkventory.Data;

public static class ConfigurationExtensions
{
    public static void ConfigureInkventoryData(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureRaftelData<InkventoryDbContext>(configuration);
    }
}
 