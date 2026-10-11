using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Commands;
using Raftel.Application.Exceptions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that converts a <see cref="ConcurrencyConflictException"/> raised while saving into a
/// failed <see cref="Result"/> with an error of type <see cref="ErrorType.Conflict"/>.
/// </summary>
/// <remarks>
/// Register it outside <see cref="TransactionMiddleware{TRequest}"/> so the transaction has already been
/// rolled back by the time the failed result is produced. Any other exception propagates unchanged.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand"/>.</typeparam>
public class ConcurrencyMiddleware<TRequest> : ICommandMiddleware<TRequest>
    where TRequest : ICommand
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Result.Failure(ConcurrencyErrors.Conflict(exception.EntityNames));
        }
    }
}

/// <summary>
/// Middleware that converts a <see cref="ConcurrencyConflictException"/> raised while saving into a
/// failed typed <see cref="Result{TResult}"/> with an error of type <see cref="ErrorType.Conflict"/>.
/// </summary>
/// <remarks>
/// See <see cref="ConcurrencyMiddleware{TRequest}"/> for the full behaviour - this variant mirrors it
/// for <see cref="ICommand{TResult}"/>.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
public class ConcurrencyMiddleware<TRequest, TResult> : ICommandMiddleware<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(TRequest request,
        RequestHandlerDelegate<Result<TResult>> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Result.Failure<TResult>(ConcurrencyErrors.Conflict(exception.EntityNames));
        }
    }
}
