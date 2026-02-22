using System.Text.Json;
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
            TCommand parsedCommand;
            try
            {
                parsedCommand = await context.Request.ReadFromJsonAsync<TCommand>();
            }
            catch (JsonException)
            {
                return Results.Problem(
                    detail: "The request body contains invalid JSON syntax.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid JSON payload");
            }

            if (parsedCommand is null)
            {
                return Results.Problem(
                    detail: "The request body must not be null.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid JSON payload");
            }

            var result = await dispatcher.DispatchAsync(parsedCommand);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        }
    }
}