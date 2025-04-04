﻿using Raftel.Core.Results;

namespace Raftel.Application.Commands;

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command);
}