using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that ensures a unit of work is committed
/// only if the preceding operation was successful.
/// </summary>
/// <remarks>
/// The commit always runs with <see cref="CancellationToken.None"/>, regardless of the request's
/// cancellation token. Cancelling mid-<c>SaveChanges</c> would leave the transaction in an
/// indeterminate state and can abort in-flight domain event dispatch, so once the handler has
/// produced a successful result the commit is deliberately not cancellable.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand"/>.</typeparam>
/// <param name="unitOfWork">The unit of work instance used to perform the commit.</param>
public class UnitOfWorkMiddleware<TRequest>(IUnitOfWork unitOfWork) : ICommandMiddleware<TRequest>
    where TRequest : ICommand
{
    /// <summary>
    /// Handles the request by executing the next delegate in the middleware pipeline
    /// and commits the unit of work if the result is successful.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response.IsSuccess)
        {
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        return response;
    }
}

/// <summary>
/// Middleware that ensures a unit of work is committed
/// only if the preceding operation was successful, for commands that return a typed result.
/// </summary>
/// <remarks>
/// The commit always runs with <see cref="CancellationToken.None"/>, regardless of the request's
/// cancellation token. Cancelling mid-<c>SaveChanges</c> would leave the transaction in an
/// indeterminate state and can abort in-flight domain event dispatch, so once the handler has
/// produced a successful result the commit is deliberately not cancellable.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
/// <param name="unitOfWork">The unit of work instance used to perform the commit.</param>
public class UnitOfWorkMiddleware<TRequest, TResult>(IUnitOfWork unitOfWork)
    : ICommandMiddleware<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    /// <summary>
    /// Handles the request by executing the next delegate in the middleware pipeline
    /// and commits the unit of work if the result is successful.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result{TResult}"/> indicating the outcome of the operation.</returns>
    public async Task<Result<TResult>> HandleAsync(TRequest request,
        RequestHandlerDelegate<Result<TResult>> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response.IsSuccess)
        {
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        return response;
    }
}