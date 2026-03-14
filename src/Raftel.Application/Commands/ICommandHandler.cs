using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

/// <summary>
/// Defines a handler for processing a command of type <typeparamref name="TCommand"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Defines a handler for processing a command of type <typeparamref name="TCommand"/>
/// that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
public interface ICommandHandler<TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>;