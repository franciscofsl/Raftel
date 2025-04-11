using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandDispatcher"/> that delegates command execution
/// to the <see cref="IRequestDispatcher"/> infrastructure.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IRequestDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
    /// </summary>
    /// <param name="dispatcher">The internal dispatcher used to route commands.</param>
    public CommandDispatcher(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public Task<Result> DispatchAsync<TCommand>(TCommand command)
        where TCommand : ICommand
        => _dispatcher.DispatchAsync<TCommand, Result>(command);
}