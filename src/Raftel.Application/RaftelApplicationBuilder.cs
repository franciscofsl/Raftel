using System.Reflection;

namespace Raftel.Application;

internal sealed class RaftelApplicationBuilder : IRaftelApplicationBuilder
{
    public List<Assembly> Assemblies { get; } = new();

    public void RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }
    }
}