using System.Reflection;

namespace Raftel.Application;

/// <summary>
/// Defines the contract for building and configuring the application by registering services
/// and middleware components.
/// </summary>
public interface IRaftelApplicationBuilder
{
    /// <summary>
    /// Registers all services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the services to register.</param>
    void RegisterServicesFromAssembly(Assembly assembly);

    /// <summary>
    /// Adds a global middleware type to the application pipeline.
    /// </summary>
    /// <param name="middleware">The type of the middleware to add.</param>
    void AddGlobalMiddleware(Type middleware);

    /// <summary>
    /// Adds a middleware type specific to command handling to the application pipeline.
    /// </summary>
    /// <param name="middleware">The type of the middleware to add.</param>
    void AddCommandMiddleware(Type middleware);

    /// <summary>
    /// Adds a middleware type specific to query handling to the application pipeline.
    /// </summary>
    /// <param name="openMiddleware">The type of the middleware to add.</param>
    void AddQueryMiddleware(Type openMiddleware);
}