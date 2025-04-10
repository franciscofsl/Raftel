using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

public interface ICommandDispatcher
{
    Task<Result> DispatchAsync<TCommand>(TCommand command)
        where TCommand : ICommand;
}