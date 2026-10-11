using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

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

        async Task<IResult> Handler(HttpContext context, ICommandDispatcher dispatcher,
            CancellationToken cancellationToken)
        {
            TCommand parsedCommand;
            try
            {
                parsedCommand = await context.Request.ReadFromJsonAsync<TCommand>(cancellationToken);
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

            var precondition = IfMatchPrecondition.Evaluate(context, typeof(TCommand));
            if (precondition.Rejection is not null)
            {
                return precondition.Rejection;
            }

            var result = await dispatcher.DispatchAsync(parsedCommand, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : ToProblem(result.Error, precondition);
        }
    }

    public static void MapCommandEndpointWithResult<TCommand, TResult>(RouteGroupBuilder group,
        CommandDefinition command) where TCommand : ICommand<TResult>
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

        async Task<IResult> Handler(HttpContext context, ICommandDispatcher dispatcher,
            CancellationToken cancellationToken)
        {
            TCommand parsedCommand;
            try
            {
                parsedCommand = await context.Request.ReadFromJsonAsync<TCommand>(cancellationToken);
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

            var precondition = IfMatchPrecondition.Evaluate(context, typeof(TCommand));
            if (precondition.Rejection is not null)
            {
                return precondition.Rejection;
            }

            var result = await dispatcher.DispatchAsync<TCommand, TResult>(parsedCommand, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToProblem(result.Error, precondition);
            }

            return command.CreatedRouteName is not null
                ? Results.CreatedAtRoute(command.CreatedRouteName, new { id = result.Value }, result.Value)
                : Results.Ok(result.Value);
        }
    }

    private static IResult ToProblem(Error error, IfMatchPrecondition.Outcome precondition) =>
        precondition.ExpectedVersionSet && error.Code == ConcurrencyErrors.ConflictCode
            ? ErrorResults.ToPreconditionFailedProblem(error)
            : ErrorResults.ToProblem(error);
}