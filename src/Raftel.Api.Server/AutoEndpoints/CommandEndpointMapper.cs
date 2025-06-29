using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raftel.Application.Commands;

namespace Raftel.Api.Server.AutoEndpoints;

public static class CommandEndpointMapper
{
    public static void MapCommandEndpoint<TCommand>(RouteGroupBuilder group,
        CommandDefinition command) where TCommand : ICommand
    {
        var endpoint = command.Method switch
        {
            var m when m == HttpMethod.Post => group.MapPost(command.Route, Handler),
            var m when m == HttpMethod.Put => group.MapPut(command.Route, Handler),
            var m when m == HttpMethod.Delete => group.MapDelete(command.Route, Handler),
            _ => throw new NotSupportedException($"HTTP method {command.Method} not supported for commands")
        };

        endpoint
            .WithName($"{command.Method}_{typeof(TCommand).Name}")
            .WithOpenApi()
            .AuthorizeByRequiresPermissionAttribute<TCommand>();

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