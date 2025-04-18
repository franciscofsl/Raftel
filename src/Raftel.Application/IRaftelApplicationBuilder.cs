using System.Reflection;

namespace Raftel.Application;

public interface IRaftelApplicationBuilder
{
    void RegisterServicesFromAssembly(Assembly assembly);
}