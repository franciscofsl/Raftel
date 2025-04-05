using Microsoft.Extensions.DependencyInjection;
using Raftel.Infrastructure;

namespace Raftel.Inkventory.Infrastructure;

public static class ConfigurationExtensions
{
    public static void ConfigureInkventoryInfrastructure(this IServiceCollection services)
    {
        services.ConfigureRaftelInfrastructure();
    }
}