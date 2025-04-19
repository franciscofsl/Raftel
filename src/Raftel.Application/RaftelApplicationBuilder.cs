using System.Reflection;

namespace Raftel.Application;

public sealed class RaftelApplicationBuilder : IRaftelApplicationBuilder
{
    public List<Assembly> Assemblies { get; } = new();
    public List<Type> GlobalMiddlewares { get; } = new();
    public List<Type> CommandMiddlewares { get; } = new();
    public List<Type> QueryMiddlewares { get; } = new();

    public void RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }
    }

    public void AddGlobalMiddleware(Type openMiddleware)
    {
        GlobalMiddlewares.Add(openMiddleware);
    }

    public void AddCommandMiddleware(Type openMiddleware)
    { 
        CommandMiddlewares.Add(openMiddleware);
    }

    public void AddQueryMiddleware(Type openMiddleware)
    {
        QueryMiddlewares.Add(openMiddleware);
    } 
}