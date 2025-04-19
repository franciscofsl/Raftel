using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandDispatcher"/> that delegates command execution
/// to the <see cref="IRequestDispatcher"/> infrastructure.
/// </summary>
public class CommandDispatcher(IRequestDispatcher dispatcher) : ICommandDispatcher
{
    /// <inheritdoc />
    public Task<Result> DispatchAsync<TCommand>(TCommand command)
        where TCommand : ICommand
        => dispatcher.DispatchAsync<TCommand, Result>(command);
}