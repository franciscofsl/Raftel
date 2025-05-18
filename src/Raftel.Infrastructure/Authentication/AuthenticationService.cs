using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.LogInUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;

namespace Raftel.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    UserManager<IdentityUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IClaimsPrincipalFactory claimsPrincipalFactory) : IAuthenticationService
{
    public async Task<Result> RegisterAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var identityUser = new IdentityUser
        {
            UserName = user.Email,
            Email = user.Email,
        };

        var result = await userManager.CreateAsync(identityUser, password);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errorMessage = result.Errors.Select(e => e.Description).FirstOrDefault();
        return Result.Failure(new Error("User.CantRegister", errorMessage));
    }

    public async Task<Result<LogInResult>> LogInAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        var req = httpContextAccessor.HttpContext.GetOpenIddictServerRequest()!;
        if (!req.IsPasswordGrantType())
        {
            return Result<LogInResult>.Failure(new Error("InvalidGrantType", "The specified grant type is not supported."));
        }

        var user = await userManager.FindByNameAsync(req.Username);
        if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
        {
            return Result<LogInResult>.Failure(new Error("User.CantLogin", "The specified username or password is incorrect."));
        }

        var principal = await claimsPrincipalFactory.CreateAsync(user);

        principal.SetScopes(
            OpenIddictConstants.Permissions.Prefixes.Scope + "api",
            OpenIddictConstants.Scopes.OfflineAccess);

        return new LogInResult(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}