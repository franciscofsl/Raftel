using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using Raftel.Application.Abstractions.Authentication;

namespace Raftel.Infrastructure.Authentication;

internal sealed class CurrentHttpUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => accessor.HttpContext?.User?.FindFirst(OpenIddictConstants.Claims.Subject) is not null
        ? new Guid(accessor.HttpContext.User.FindFirst(OpenIddictConstants.Claims.Subject)!.Value)
        : null;

    public string? UserName => accessor.HttpContext?.User?.FindFirst(OpenIddictConstants.Claims.Name)?.Value;

    public IEnumerable<string> Roles =>
        accessor.HttpContext?.User?.Claims
            .Where(c => c.Type == "role")
            .Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    private IEnumerable<string> Permissions =>
        accessor.HttpContext?.User?.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool HasPermission(string permission) =>
        IsAuthenticated && Permissions.Contains(permission);
}

