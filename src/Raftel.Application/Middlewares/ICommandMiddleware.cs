using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Represents a middleware specific to command handling in the request pipeline.
/// Inherits from <see cref="IGlobalMiddleware{TRequest, TResponse}"/> with a fixed response type of <see cref="Result"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the command request.</typeparam>
public interface ICommandMiddleware<TRequest> : IGlobalMiddleware<TRequest, Result>
    where TRequest : ICommand
{
}

/// <summary>
/// Represents a middleware specific to command handling in the request pipeline
/// for commands that return a typed result.
/// </summary>
/// <typeparam name="TRequest">The type of the command request.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
public interface ICommandMiddleware<TRequest, TResult> : IGlobalMiddleware<TRequest, Result<TResult>>
    where TRequest : ICommand<TResult>
{
}