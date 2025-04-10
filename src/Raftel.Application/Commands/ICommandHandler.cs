using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;