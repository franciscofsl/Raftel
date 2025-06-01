using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Infrastructure.Authentication;

internal sealed class ClaimsPrincipalFactory(
    UserManager<IdentityUser> userManager,
    IRolesRepository rolesRepository)
    : IClaimsPrincipalFactory
{
    public async Task<ClaimsPrincipal> CreateAsync(IdentityUser user)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Name, user.UserName));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Email, user.Email ?? ""));

        var rolesFromUser = await userManager.GetRolesAsync(user);
        var roles = await rolesRepository.ListAllAsync(_ => rolesFromUser.Contains(_.Name));
        var permissions = roles.SelectMany(_ => _.PermissionNames()).ToArray();

        foreach (var role in rolesFromUser)
        {
            identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Role, role));
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(CreateClaim("permission", permission));
        }

        return new ClaimsPrincipal(identity);
    }

    private static Claim CreateClaim(string type, string value)
    {
        var claim = new Claim(type, value);
        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        return claim;
    }
}