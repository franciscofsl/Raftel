using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

/// <summary>
/// Dispatches a command to its corresponding handler.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command asynchronously.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <param name="command">The command instance to process.</param>
    /// <param name="token"></param>
    /// <returns>A <see cref="Result"/> representing the outcome of the command execution.</returns>
    Task<Result> DispatchAsync<TCommand>(TCommand command, CancellationToken token = default)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command asynchronously and returns a typed result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
    /// <param name="command">The command instance to process.</param>
    /// <param name="token"></param>
    /// <returns>A <see cref="Result{TResult}"/> representing the outcome of the command execution.</returns>
    Task<Result<TResult>> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken token = default)
        where TCommand : ICommand<TResult>;
}