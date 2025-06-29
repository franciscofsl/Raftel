using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Raftel.Application.Authorization;

namespace Raftel.Api.Server.AutoEndpoints;

internal static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder AuthorizeByRequiresPermissionAttribute<TRequest>(
        this RouteHandlerBuilder endpoint)
    {
        var permissionAttributes = typeof(TRequest)
            .GetCustomAttributes<RequiresPermissionAttribute>(true)
            .ToArray();

        return permissionAttributes.Length == 0
            ? endpoint.AllowAnonymous()
            : endpoint.RequireAuthorization();
    }
}