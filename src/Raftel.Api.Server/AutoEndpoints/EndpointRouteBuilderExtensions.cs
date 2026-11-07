using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Authorization;

namespace Raftel.Api.Server.AutoEndpoints;

internal static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="RequiresPermissionAttribute"/> declared on <typeparamref name="TRequest"/> to
    /// the endpoint: an anonymous caller is challenged (401) and an authenticated caller missing any of the
    /// declared permissions is forbidden (403) before the request reaches the command/query pipeline.
    /// Requests without the attribute stay anonymous.
    /// </summary>
    public static RouteHandlerBuilder AuthorizeByRequiresPermissionAttribute<TRequest>(
        this RouteHandlerBuilder endpoint)
    {
        var requiredPermissions = typeof(TRequest)
            .GetCustomAttributes<RequiresPermissionAttribute>(true)
            .Select(attribute => attribute.Permission)
            .ToArray();

        if (requiredPermissions.Length == 0)
        {
            return endpoint.AllowAnonymous();
        }

        return endpoint.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => HoldsEveryPermission(context, requiredPermissions)));
    }

    private static bool HoldsEveryPermission(AuthorizationHandlerContext context, string[] requiredPermissions)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return false;
        }

        var currentUser = httpContext.RequestServices.GetService<ICurrentUser>();
        return currentUser is not null && requiredPermissions.All(currentUser.HasPermission);
    }
}
