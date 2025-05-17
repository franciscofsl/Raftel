using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raftel.Application.Commands;

namespace Raftel.Api.Server.AutoEndpoints;

public static class CommandEndpointMapper
{
    public static void MapCommandEndpoint<TCommand>(RouteGroupBuilder group,
        string route,
        HttpMethod method) where TCommand : ICommand
    {
        var endpoint = method switch
        {
            var m when m == HttpMethod.Post => group.MapPost(route, Handler),
            var m when m == HttpMethod.Put => group.MapPut(route, Handler),
            var m when m == HttpMethod.Delete => group.MapDelete(route, Handler),
            _ => throw new NotSupportedException($"HTTP method {method} not supported for commands")
        };

        endpoint
            .WithName($"{method.Method}_{typeof(TCommand).Name}")
            .WithOpenApi();
        return;

        async Task<IResult> Handler(HttpContext context, ICommandDispatcher dispatcher)
        {
            var command = await context.Request.ReadFromJsonAsync<TCommand>()
                          ?? throw new InvalidOperationException("Invalid command payload");

            var result = await dispatcher.DispatchAsync(command);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        }
    }
}