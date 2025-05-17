using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raftel.Application.Abstractions;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Raftel.Shared.Extensions;

namespace Raftel.Api.Server.AutoEndpoints;

public static class QueryEndpointMapper
{
    public static void MapQueryEndpoint<TRequest, TResult>(
        RouteGroupBuilder group,
        string route,
        HttpMethod method)
        where TRequest : IQuery<TResult>
    {
        var endpoint = method switch
        {
            var m when m == HttpMethod.Get => group.MapGet(route, Handler),
            var m when m == HttpMethod.Post => group.MapPost(route, Handler),
            _ => throw new NotSupportedException($"HTTP method {method} not supported")
        };

        endpoint
            .WithName($"{method.Method}_{typeof(TRequest).Name}")
            .WithOpenApi(operation =>
            {
                operation.Parameters = ApiParametersBuilder.Calculate<TRequest>(route);
                return operation;
            });
        return;

        async Task<IResult> Handler(HttpContext context, IRequestDispatcher dispatcher)
        {
            var request = BuildRequestFromRouteAndQuery<TRequest>(context);
            var result = await dispatcher.DispatchAsync<TRequest, Result<TResult>>(request);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }
    }
    
    private static TRequest BuildRequestFromRouteAndQuery<TRequest>(HttpContext context)
    {
        var constructor = typeof(TRequest).GetConstructors().FirstOrDefault()
                          ?? throw new InvalidOperationException(
                              $"No public constructor found for {typeof(TRequest).Name}");

        var args = constructor
            .GetParameters()
            .Select(param => ApiParamValueToObject(context, param))
            .ToArray();

        return (TRequest)constructor.Invoke(args);
    }

    private static object? ApiParamValueToObject(HttpContext context, ParameterInfo param)
    {
        var name = param.Name!;
        if (context.Request.RouteValues.TryGetValue(name, out var routeValue) && routeValue != null)
        {
            return ConvertSimpleType(routeValue, param.ParameterType);
        }

        if (context.Request.Query.TryGetValue(name, out var queryValue) &&
            !string.IsNullOrWhiteSpace(queryValue))
        {
            return ConvertSimpleType(queryValue.ToString(), param.ParameterType);
        }

        if (param.ParameterType.IsNullable())
        {
            return null;
        }

        throw new InvalidOperationException($"Missing required parameter '{name}'");
    }

    private static object? ConvertSimpleType(object value, Type targetType)
    {
        if (targetType == typeof(Guid) && value is string s)
        {
            return Guid.Parse(s);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.ToString()!);
        }

        return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
    }
}