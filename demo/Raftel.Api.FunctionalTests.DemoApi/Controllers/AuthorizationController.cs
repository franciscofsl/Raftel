using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Raftel.Infrastructure.Authentication;

namespace Raftel.Api.FunctionalTests.DemoApi.Controllers;

[ApiController]
public class AuthorizationController : Controller
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _users;
    private readonly IClaimsPrincipalFactory _factory;

    public AuthorizationController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users,
        IClaimsPrincipalFactory factory)
    {
        _signIn = signIn;
        _users = users;
        _factory = factory;
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
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await _factory.CreateAsync(user);

            principal.SetScopes(
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Scopes.OfflineAccess);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }
}