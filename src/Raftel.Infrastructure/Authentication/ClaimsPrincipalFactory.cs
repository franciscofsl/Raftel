using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Raftel.Infrastructure.Authentication;

internal sealed class ClaimsPrincipalFactory(UserManager<IdentityUser> userManager)
    : IClaimsPrincipalFactory
{
    public async Task<ClaimsPrincipal> CreateAsync(IdentityUser user)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Name, user.UserName));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Email, user.Email ?? ""));

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Role, role));
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