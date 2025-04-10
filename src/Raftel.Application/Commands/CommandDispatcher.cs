using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

public class CommandDispatcher(IRequestDispatcher dispatcher) : ICommandDispatcher
{
    public Task<Result> DispatchAsync<TCommand>(TCommand command)
        where TCommand : ICommand
        => dispatcher.DispatchAsync<TCommand, Result>(command);
}