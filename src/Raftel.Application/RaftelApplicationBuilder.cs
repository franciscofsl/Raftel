using System.Reflection;

namespace Raftel.Application;

/// <summary>
/// Builder class for configuring and registering application services, middlewares, and assemblies.
/// </summary>
public sealed class RaftelApplicationBuilder : IRaftelApplicationBuilder
{
    /// <summary>
    /// Gets the list of assemblies registered in the application.
    /// </summary>
    public List<Assembly> Assemblies { get; } = new();

    /// <summary>
    /// Gets the list of globally registered middleware types.
    /// </summary>
    public List<Type> GlobalMiddlewares { get; } = new();

    /// <summary>
    /// Gets the list of middleware types specific to command handling.
    /// </summary>
    public List<Type> CommandMiddlewares { get; } = new();

    /// <summary>
    /// Gets the list of middleware types specific to query handling.
    /// </summary>
    public List<Type> QueryMiddlewares { get; } = new();

    /// <summary>
    /// Gets the pagination options configured for the application.
    /// </summary>
    public PaginationOptions PaginationOptions { get; } = new();

    /// <summary>
    /// Gets the authorization options configured for the application.
    /// </summary>
    public AuthorizationOptions AuthorizationOptions { get; } = new();

    /// <summary>
    /// Registers all services from the specified assembly if it has not already been registered.
    /// </summary>
    /// <param name="assembly">The assembly to register services from.</param>
    public void RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }
    }

    /// <summary>
    /// Adds a global middleware type to the application.
    /// </summary>
    /// <param name="openMiddleware">The type of the middleware to add.</param>
    public void AddGlobalMiddleware(Type openMiddleware)
    {
        GlobalMiddlewares.Add(openMiddleware);
    }

    /// <summary>
    /// Adds a middleware type specific to command handling to the application.
    /// </summary>
    /// <param name="openMiddleware">The type of the middleware to add.</param>
    public void AddCommandMiddleware(Type openMiddleware)
    {
        CommandMiddlewares.Add(openMiddleware);
    }

    /// <summary>
    /// Adds a middleware type specific to query handling to the application.
    /// </summary>
    /// <param name="openMiddleware">The type of the middleware to add.</param>
    public void AddQueryMiddleware(Type openMiddleware)
    {
        QueryMiddlewares.Add(openMiddleware);
    }

    /// <summary>
    /// Configures the default and maximum page size applied by paged queries.
    /// </summary>
    /// <param name="configure">A callback that mutates the pagination options.</param>
    public void ConfigurePagination(Action<PaginationOptions> configure)
    {
        configure(PaginationOptions);
    }

    /// <summary>
    /// Configures how the permission-authorization pipeline reports failures.
    /// </summary>
    /// <param name="configure">A callback that mutates the authorization options.</param>
    public void ConfigureAuthorization(Action<AuthorizationOptions> configure)
    {
        configure(AuthorizationOptions);
    }
}