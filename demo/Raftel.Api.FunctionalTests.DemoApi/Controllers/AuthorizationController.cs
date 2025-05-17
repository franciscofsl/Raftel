using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Raftel.Api.FunctionalTests.DemoApi.Controllers;

[ApiController]
public class AuthorizationController : Controller
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _users;

    public AuthorizationController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    [AllowAnonymous]
    public async Task<IActionResult> Exchange()
    {
        var req = HttpContext.GetOpenIddictServerRequest()!;
        if (req.IsPasswordGrantType())
        {
            var user = await _users.FindByNameAsync(req.Username);
            if (user == null || !await _users.CheckPasswordAsync(user, req.Password))
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var principal = await _signIn.CreateUserPrincipalAsync(user);

            principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);

            principal.SetScopes(
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Scopes.OfflineAccess);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }
}