using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Validators;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that performs validation on the incoming request using all registered <see cref="Validator{TRequest}"/> instances.
/// If any validation rule fails, a failed <typeparamref name="TResponse"/> carrying a <see cref="ValidationError"/> is
/// returned and the request pipeline is short-circuited.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the expected response.</typeparam>
/// <param name="validators">A collection of validators for the given <typeparamref name="TRequest"/>.</param>
public class ValidationMiddleware<TRequest, TResponse>(IEnumerable<Validator<TRequest>> validators)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    /// <summary>
    /// Handles the request by validating it before invoking the next delegate in the pipeline.
    /// If validation passes, the request continues to the next handler.
    /// If validation fails, a failed <typeparamref name="TResponse"/> is returned without invoking <paramref name="next"/>.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The response returned by the next handler, if validation succeeds; otherwise a failed response.</returns>
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var allErrors = validators
            .Select(validator => validator.Validate(request))
            .SelectMany(validationResult => validationResult.Errors)
            .ToList();

        if (allErrors.Count == 0)
        {
            return next(cancellationToken);
        }

        return Task.FromResult(ResultFactory.CreateFailure<TResponse>(ValidationError.FromErrors(allErrors)));
    }
}
