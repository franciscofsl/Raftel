using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Raftel.Shared.Extensions;

namespace Raftel.Api.AutoEndpoints;

public static class ApiParametersBuilder
{
    public static IList<OpenApiParameter> Calculate<TRequest>(string route)
    {
        var parameters = new List<OpenApiParameter>();
        var routeParameters = Regex.Matches(route, "{(.*?)}")
            .Select(m => m.Groups[1].Value)
            .ToArray();

        var properties = typeof(TRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, paramType) in properties)
        {
            var parameter = RequestParameterToOpenApiParameter(paramType, routeParameters, name);
            parameters.Add(parameter);
        }

        return parameters;
    }

    private static OpenApiParameter RequestParameterToOpenApiParameter(Type paramType,
        string[] routeParameters,
        string name)
    {
        var (type, format) = MapToOpenApiType(paramType);
        var parameterLocation = routeParameters.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? ParameterLocation.Path
            : ParameterLocation.Query;
        return new OpenApiParameter
        {
            Name = name.ToCamelCase(),
            In = parameterLocation,
            Required = parameterLocation is ParameterLocation.Path,
            Schema = new OpenApiSchema
            {
                Type = type,
                Format = format
            }
        };
    }

    private static (string Type, string? Format) MapToOpenApiType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(Guid))
            return ("string", "uuid");
        if (type == typeof(DateTime))
            return ("string", "date-time");
        if (type == typeof(int) || type == typeof(long))
            return ("integer", null);
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return ("number", "double");
        if (type == typeof(bool))
            return ("boolean", null);

        return ("string", null);
    }
}