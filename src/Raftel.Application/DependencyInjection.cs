using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Authorization;
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
        RegisterPermissionSets(services, builder.Assemblies);

        services.AddSingleton(builder.PaginationOptions);
        services.AddSingleton(builder.AuthorizationOptions);
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddScoped<IRequestEvent, RequestEvent>();
        services.TryAddScoped<ICurrentUser, AnonymousCurrentUser>();
    }

    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var handlerTypes = new[]
        {
            typeof(ICommandHandler<>), typeof(ICommandHandler<,>), typeof(IQueryHandler<,>),
            typeof(IRequestHandler<,>), typeof(IDomainEventHandler<>)
        };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                RegisterHandlers(services, type, handlerTypes);
                RegisterValidators(services, type);
                RegisterSeeders(services, type);
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

    private static void RegisterSeeders(IServiceCollection services, Type type)
    {
        if (type.IsAbstract || type.IsInterface || !typeof(IDataSeeder).IsAssignableFrom(type))
        {
            return;
        }

        services.AddScoped(typeof(IDataSeeder), type);
    }

    private static void RegisterPermissionSets(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var scanned = assemblies.Append(typeof(PermissionSetAttribute).Assembly);
        services.AddSingleton<IPermissionSetProvider>(new PermissionSetProvider(scanned));
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
        var globalMiddlewares = WithPermissionAuthorization(builder.GlobalMiddlewares);
        var middlewareRegistry = new MiddlewareRegistry(globalMiddlewares, builder.CommandMiddlewares,
            builder.QueryMiddlewares);
        services.AddSingleton(middlewareRegistry);

        foreach (var type in globalMiddlewares)
        {
            services.AddScoped(typeof(IGlobalMiddleware<,>), type);
        }

        foreach (var type in builder.CommandMiddlewares)
        {
            var genericArgCount = type.GetGenericArguments().Length;
            if (genericArgCount == 2)
            {
                services.AddScoped(typeof(ICommandMiddleware<,>), type);
            }
            else
            {
                services.AddScoped(typeof(ICommandMiddleware<>), type);
            }
        }

        foreach (var type in builder.QueryMiddlewares)
        {
            services.AddScoped(typeof(IQueryMiddleware<,>), type);
        }
    }

    /// <summary>
    /// Guarantees that <see cref="PermissionAuthorizationMiddleware{TRequest,TResponse}"/> is always part of
    /// the global pipeline, so <see cref="RequiresPermissionAttribute"/> is enforced even when the host does
    /// not register it explicitly. It is placed right after <see cref="LoggingMiddleware{TRequest,TResponse}"/>
    /// (which must stay outermost) or first otherwise, so authorization runs before validation and handlers.
    /// An explicit registration is kept where the host put it and never duplicated.
    /// </summary>
    private static List<Type> WithPermissionAuthorization(List<Type> configured)
    {
        var permissionMiddleware = typeof(PermissionAuthorizationMiddleware<,>);
        var globalMiddlewares = new List<Type>(configured);
        if (globalMiddlewares.Contains(permissionMiddleware))
        {
            return globalMiddlewares;
        }

        var loggingIndex = globalMiddlewares.IndexOf(typeof(LoggingMiddleware<,>));
        globalMiddlewares.Insert(loggingIndex + 1, permissionMiddleware);
        return globalMiddlewares;
    }
}
