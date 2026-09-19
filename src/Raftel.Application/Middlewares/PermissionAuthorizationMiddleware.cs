using System.Reflection;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that enforces permission-based authorization for requests decorated with <see cref="RequiresPermissionAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
public class PermissionAuthorizationMiddleware<TRequest, TResponse>(
    ICurrentUser currentUser,
    AuthorizationOptions authorizationOptions)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    /// <summary>
    /// Handles the request by checking if the current user has the required permissions.
    /// If a required permission is missing, a failed <typeparamref name="TResponse"/> carrying a
    /// <see cref="ErrorType.Forbidden"/> error is returned without invoking <paramref name="next"/>.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next middleware or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The response from the next handler if authorization succeeds; otherwise a failed response.</returns>
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requiredPermissions = typeof(TRequest)
            .GetCustomAttributes<RequiresPermissionAttribute>(true)
            .Select(attr => attr.Permission)
            .ToArray();

        if (requiredPermissions.Length == 0)
        {
            return next(cancellationToken);
        }

        var missingPermissions = requiredPermissions.Where(p => !currentUser.HasPermission(p)).ToArray();

        if (missingPermissions.Length == 0)
        {
            return next(cancellationToken);
        }

        var message = authorizationOptions.IncludeMissingPermissionsInError
            ? $"Missing permission: {string.Join(", ", missingPermissions)}"
            : "The current user does not have permission to perform this action.";

        var error = Error.Forbidden("Authorization.Forbidden", message);

        return Task.FromResult(ResultFactory.CreateFailure<TResponse>(error));
    }
}
