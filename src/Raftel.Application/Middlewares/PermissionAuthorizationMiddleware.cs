using System.Reflection;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Authorization;
using Raftel.Application.Exceptions;

namespace Raftel.Application.Middlewares;

/// <summary>
/// Middleware that enforces permission-based authorization for requests decorated with <see cref="RequiresPermissionAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
public class PermissionAuthorizationMiddleware<TRequest, TResponse>(ICurrentUser currentUser)
    : IGlobalMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request by checking if the current user has the required permissions.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next middleware or handler in the pipeline.</param>
    /// <returns>The response from the next handler if authorization succeeds.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the user does not have the required permissions.</exception>
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var requiredPermissions = typeof(TRequest)
            .GetCustomAttributes<RequiresPermissionAttribute>(true)
            .Select(attr => attr.Permission)
            .ToArray();

        if (requiredPermissions.Length == 0)
        {
            return next();
        }

        foreach (var permission in requiredPermissions)
        {
            currentUser.EnsureHasPermission(permission);
        }

        return next();
    }
}
