using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Infrastructure.Authentication;

internal sealed class ClaimsPrincipalFactory(
    UserManager<IdentityUser> userManager,
    IRolesRepository rolesRepository,
    IUserTenantResolver userTenantResolver,
    ICurrentTenant currentTenant)
    : IClaimsPrincipalFactory
{
    public async Task<ClaimsPrincipal> CreateAsync(IdentityUser user)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Name, user.UserName));
        identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Email, user.Email ?? ""));

        var tenantId = await userTenantResolver.ResolveAsync(user.Id);
        if (tenantId is not null)
        {
            identity.AddClaim(CreateClaim(RaftelClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        var rolesFromUser = await userManager.GetRolesAsync(user);

        // Roles (and so permissions) are resolved within the user's own tenant, never the tenant requested by
        // the caller, so a login request cannot pick up roles of another tenant.
        List<Role> roles;
        using (currentTenant.Change(tenantId))
        {
            roles = await rolesRepository.ListAllAsync(_ => rolesFromUser.Contains(_.Name));
        }

        var permissions = roles.SelectMany(_ => _.PermissionNames()).Distinct().ToArray();

        foreach (var role in rolesFromUser)
        {
            identity.AddClaim(CreateClaim(OpenIddictConstants.Claims.Role, role));
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(CreateClaim(RaftelClaimTypes.Permission, permission));
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
