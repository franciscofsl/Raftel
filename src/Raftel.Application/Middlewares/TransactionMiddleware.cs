using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that wraps command handling in a single explicit transaction: committed when the
/// result succeeds, rolled back when it fails or the handler throws.
/// </summary>
/// <remarks>
/// Placed outside <see cref="UnitOfWorkMiddleware{TRequest}"/> so the transaction also covers
/// auditing and domain event dispatch, both of which ride on <c>SaveChanges</c>. If a transaction
/// is already active on the unit of work, this middleware does nothing beyond invoking the next
/// delegate unchanged - only the outermost <see cref="TransactionMiddleware{TRequest}"/>
/// for a given unit of work owns the physical transaction.
/// Commit and rollback always run with <see cref="CancellationToken.None"/>: cancelling mid-commit
/// or mid-rollback would leave the transaction in an indeterminate state.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand"/>.</typeparam>
/// <param name="unitOfWork">The unit of work used to begin, commit and roll back the transaction.</param>
public class TransactionMiddleware<TRequest>(IUnitOfWork unitOfWork) : ICommandMiddleware<TRequest>
    where TRequest : ICommand
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            return await next(cancellationToken);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return result;
            }

            await transaction.CommitAsync(CancellationToken.None);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}

/// <summary>
/// Middleware that wraps command handling in a single explicit transaction, for commands that
/// return a typed result: committed when the result succeeds, rolled back when it fails or the
/// handler throws.
/// </summary>
/// <remarks>
/// See <see cref="TransactionMiddleware{TRequest}"/> for the full behaviour - this variant mirrors
/// it for <see cref="ICommand{TResult}"/>.
/// </remarks>
/// <typeparam name="TRequest">The type of the request implementing <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the command.</typeparam>
/// <param name="unitOfWork">The unit of work used to begin, commit and roll back the transaction.</param>
public class TransactionMiddleware<TRequest, TResult>(IUnitOfWork unitOfWork)
    : ICommandMiddleware<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(TRequest request,
        RequestHandlerDelegate<Result<TResult>> next, CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            return await next(cancellationToken);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return result;
            }

            await transaction.CommitAsync(CancellationToken.None);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
