using System.Reflection;

namespace Raftel.Application;

public interface IRaftelApplicationBuilder
{
    void RegisterServicesFromAssembly(Assembly assembly);
    void AddGlobalMiddleware(Type middleware);
    void AddCommandMiddleware(Type middleware);
    void AddQueryMiddleware(Type openMiddleware);
}