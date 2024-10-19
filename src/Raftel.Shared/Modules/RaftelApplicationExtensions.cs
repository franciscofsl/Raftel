using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Raftel.Shared.Modules;

public static class RaftelApplicationExtensions
{
    public static TRaftelApplication AddRaftelApplication<TRaftelApplication>(this IServiceCollection services, IConfiguration configuration)
        where TRaftelApplication : RaftelApplication
    {
        var application = Activator.CreateInstance<TRaftelApplication>();

        application.ConfigureServices(services, configuration);
        application.ConfigureModules(services, configuration);

        return application;
    }
}
