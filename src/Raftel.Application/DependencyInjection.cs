using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Middlewares;
using Raftel.Application.Commands;
using Raftel.Application.Queries;

namespace Raftel.Application;

public static class DependencyInjection
{
    public static void AddRaftelApplication(this IServiceCollection services,
        Action<IRaftelApplicationBuilder> configure)
    {
        var builder = new RaftelApplicationBuilder();
        configure(builder);
        
        RegisterHandlers(services, assemblies: builder.Assemblies);
        
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped(typeof(IRequestMiddleware<,>), typeof(UnitOfWorkMiddleware<,>));
        services.AddScoped(typeof(IRequestMiddleware<,>), typeof(ValidationMiddleware<,>));
    }
    
    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var handlerTypes = new[] { typeof(ICommandHandler<>), typeof(IQueryHandler<,>) , typeof(IRequestHandler<,>) };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && handlerTypes.Contains(iface.GetGenericTypeDefinition()))
                        services.AddScoped(iface, type);
                }
            }
        }
    }
}