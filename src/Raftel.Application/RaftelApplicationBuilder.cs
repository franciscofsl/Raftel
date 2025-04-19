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
}