using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.AutoEndpoints;

public static class AutoEndpointExtensions
{
    public static IEndpointRouteBuilder AddQueryEndpoint<TRequest, TResult>(
        this IEndpointRouteBuilder app,
        string route,
        HttpMethod method)
        where TRequest : IRequest<Result<TResult>>
    {
        var endpoint = method switch
        {
            var m when m == HttpMethod.Get => app.MapGet(route, Handler),
            var m when m == HttpMethod.Post => app.MapPost(route, Handler),
            _ => throw new NotSupportedException($"HTTP method {method} not supported")
        };
 
        endpoint
            .WithName($"{method.Method}_{typeof(TRequest).Name}")
            .WithOpenApi(operation =>
            {
                operation.Parameters = ApiParametersBuilder.Calculate<TRequest>(route);
                return operation;
            });

        return app;

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
        var requestType = typeof(TRequest);
        var constructor = requestType.GetConstructors().FirstOrDefault();
        if (constructor == null)
            throw new InvalidOperationException($"No public constructor found for {requestType.Name}");

        var parameters = constructor.GetParameters();

        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var name = param.Name!;

            object? value = null;

            if (context.Request.RouteValues.TryGetValue(name, out var routeValue) && routeValue != null)
            {
                value = ConvertSimpleType(routeValue, param.ParameterType);
            }
            else if (context.Request.Query.TryGetValue(name, out var queryValue) &&
                     !string.IsNullOrWhiteSpace(queryValue))
            {
                value = ConvertSimpleType(queryValue.ToString(), param.ParameterType);
            }
            else if (IsNullable(param.ParameterType))
            {
                value = null;
            }
            else
            {
                throw new InvalidOperationException($"Missing required parameter '{name}'");
            }

            args[i] = value;
        }

        var instance = (TRequest)constructor.Invoke(args);
        return instance;
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

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    } 
}