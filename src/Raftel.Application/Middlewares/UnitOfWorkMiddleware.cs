using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that ensures a unit of work is committed
/// only if the preceding operation was successful.
/// </summary>
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
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next)
    {
        var response = await next();

        if (response.IsSuccess)
        {
            await unitOfWork.CommitAsync();
        }

        return response;
    }
}