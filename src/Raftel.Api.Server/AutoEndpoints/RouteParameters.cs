using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Raftel.Api.Server.AutoEndpoints;

internal sealed class RouteParameters
{
    private readonly string[] _parameters;

    private RouteParameters(string[] parameters)
    {
        _parameters = parameters;
    }

    public static RouteParameters FromRoute(string route)
    {
        var routeParameters = Regex.Matches(route, "{(.*?)}")
            .Select(m => m.Groups[1].Value)
            .ToArray();
        return new RouteParameters(routeParameters);
    }

    public ParameterLocation CalculateLocation(string paramName)
    {
        return _parameters.Contains(paramName, StringComparer.OrdinalIgnoreCase)
            ? ParameterLocation.Path
            : ParameterLocation.Query;
    }
}