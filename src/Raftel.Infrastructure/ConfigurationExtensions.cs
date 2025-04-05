using Microsoft.Extensions.DependencyInjection;
using Raftel.Core.BaseTypes;
using Raftel.Infrastructure.DomainEvents;

namespace Raftel.Infrastructure;

public static class ConfigurationExtensions
{
    public static void ConfigureRaftelInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventDispatcher, DefaultDomainEventDispatcher>();
    }
}