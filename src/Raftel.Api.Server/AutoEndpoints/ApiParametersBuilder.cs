using System.Reflection;
using Microsoft.OpenApi;
using Raftel.Shared.Extensions;

namespace Raftel.Api.Server.AutoEndpoints;

internal static class ApiParametersBuilder
{
    public static IList<IOpenApiParameter> Calculate<TRequest>(string route)
    {
        var parameters = new List<IOpenApiParameter>();
        var routeParameters = RouteParameters.FromRoute(route);

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

    private static IOpenApiParameter RequestParameterToOpenApiParameter(Type paramType,
        RouteParameters routeParameters,
        string name)
    {
        var openApiType = OpenApiType.FromType(paramType);
        var parameterLocation = routeParameters.CalculateLocation(name);
        return new OpenApiParameter
        {
            Name = name.ToCamelCase(),
            In = parameterLocation,
            Required = parameterLocation is ParameterLocation.Path,
            Schema = openApiType.ToSchema()
        };
    }
}