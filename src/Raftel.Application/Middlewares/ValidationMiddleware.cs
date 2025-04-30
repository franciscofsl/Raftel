using Raftel.Application.Exceptions;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Validators;

namespace Raftel.Application.Abstractions.Middlewares;

/// <summary>
/// Middleware that performs validation on the incoming request using all registered <see cref="Validator{TRequest}"/> instances.
/// If any validation rule fails, a <see cref="ValidationException"/> is thrown and the request pipeline is halted.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the expected response.</typeparam>
/// <param name="validators">A collection of validators for the given <typeparamref name="TRequest"/>.</param>
public class ValidationMiddleware<TRequest, TResponse>(IEnumerable<Validator<TRequest>> validators)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request by validating it before invoking the next delegate in the pipeline.
    /// If validation passes, the request continues to the next handler.
    /// If validation fails, a <see cref="ValidationException"/> is thrown containing all errors.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The delegate representing the next middleware or handler in the pipeline.</param>
    /// <returns>The response returned by the next handler, if validation succeeds.</returns>
    /// <exception cref="ValidationException">Thrown when one or more validation rules fail.</exception>
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var allErrors = validators
            .Select(_ => _.Validate(request))
            .SelectMany(_ => _.Errors)
            .ToList();

        if (allErrors.Any())
        {
            throw new ValidationException(allErrors);
        }

        return next();
    }
}