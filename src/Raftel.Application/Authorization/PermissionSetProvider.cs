using System.Reflection;

namespace Raftel.Application.Authorization;

internal sealed class PermissionSetProvider : IPermissionSetProvider
{
    public PermissionSetProvider(IEnumerable<Assembly> assemblies)
    {
        PermissionNames = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttribute<PermissionSetAttribute>(inherit: false) is not null)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<string> PermissionNames { get; }
}
