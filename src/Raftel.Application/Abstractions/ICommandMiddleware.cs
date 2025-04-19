using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions;

/// <summary>
/// Represents a middleware specific to command handling in the request pipeline.
/// Inherits from <see cref="IGlobalMiddleware{TRequest, TResponse}"/> with a fixed response type of <see cref="Result"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the command request.</typeparam>
public interface ICommandMiddleware<TRequest> : IGlobalMiddleware<TRequest, Result>
    where TRequest : ICommand
{
}