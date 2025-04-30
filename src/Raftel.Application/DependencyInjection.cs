using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Application.Queries;
using Raftel.Domain.Validators;

[assembly: InternalsVisibleTo("Raftel.Application.UnitTests")]

namespace Raftel.Application;

public static class DependencyInjection
{
    public static void AddRaftelApplication(this IServiceCollection services,
        Action<IRaftelApplicationBuilder> configure)
    {
        var builder = new RaftelApplicationBuilder();
        configure(builder);

        RegisterHandlers(services, assemblies: builder.Assemblies);
        RegisterMiddlewares(services, builder);

        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
    }

    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var handlerTypes = new[] { typeof(ICommandHandler<>), typeof(IQueryHandler<,>), typeof(IRequestHandler<,>) };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                RegisterHandlers(services, type, handlerTypes);
                RegisterValidators(services, type);
            }
        }
    }

    private static void RegisterHandlers(IServiceCollection services, Type type, Type[] handlerTypes)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return;
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && handlerTypes.Contains(iface.GetGenericTypeDefinition()))
            {
                services.AddTransient(iface, type);
            }
        }
    }

    private static void RegisterValidators(IServiceCollection services, Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return;
        }

        var validatorBase = typeof(Validator<>);
        var baseType = type.BaseType;

        if (baseType is null || !baseType.IsGenericType)
        {
            return;
        }

        var genericDefinition = baseType.GetGenericTypeDefinition();

        if (genericDefinition == validatorBase)
        {
            services.AddScoped(baseType, type);
        }
    }

    private static void RegisterMiddlewares(IServiceCollection services, RaftelApplicationBuilder builder)
    {
        var middlewareRegistry = new MiddlewareRegistry(builder.GlobalMiddlewares, builder.CommandMiddlewares,
            builder.QueryMiddlewares);
        services.AddSingleton(middlewareRegistry);

        foreach (var type in builder.GlobalMiddlewares)
        {
            services.AddScoped(typeof(IGlobalMiddleware<,>), type);
        }

        foreach (var type in builder.CommandMiddlewares)
        {
            services.AddScoped(typeof(ICommandMiddleware<>), type);
        }

        foreach (var type in builder.QueryMiddlewares)
        {
            services.AddScoped(typeof(IQueryMiddleware<,>), type);
        }
    }
}