using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.LogInUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;

namespace Raftel.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    UserManager<IdentityUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IClaimsPrincipalFactory claimsPrincipalFactory) : IAuthenticationService
{
    public async Task<Result<string>> RegisterAsync(User user, string password,
        CancellationToken cancellationToken = default)
    {
        var identityUser = new IdentityUser
        {
            UserName = user.Email,
            Email = user.Email,
        };

        var result = await userManager.CreateAsync(identityUser, password);

        if (result.Succeeded)
        {
            return Result<string>.Success(identityUser.Id);
        }

        var errorMessage = result.Errors.Select(e => e.Description).FirstOrDefault();
        return Result<string>.Failure(new Error("User.CantRegister", errorMessage));
    }

    public async Task<Result<LogInResult>> LogInAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        var req = httpContextAccessor.HttpContext.GetOpenIddictServerRequest()!;
        if (!req.IsPasswordGrantType())
        {
            return Result<LogInResult>.Failure(new Error("InvalidGrantType",
                "The specified grant type is not supported."));
        }

        var user = await userManager.FindByNameAsync(req.Username);
        if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
        {
            return Result<LogInResult>.Failure(new Error("User.CantLogin",
                "The specified username or password is incorrect."));
        }

        var principal = await claimsPrincipalFactory.CreateAsync(user);

        principal.SetScopes(
            OpenIddictConstants.Permissions.Prefixes.Scope + "api",
            OpenIddictConstants.Scopes.OfflineAccess);

        return new LogInResult(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    public async Task<Result> AssignRoleAsync(User user, Role role,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(user.Email);
        if (identityUser == null)
        {
            return Result.Failure(new Error("User.NotFound", "The specified user was not found in Identity."));
        }

        var isInRole = await userManager.IsInRoleAsync(identityUser, role.Name);
        if (isInRole)
        {
            return Result.Success();
        }

        var result = await userManager.AddToRoleAsync(identityUser, role.Name);
        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errorMessage = result.Errors.Select(e => e.Description).FirstOrDefault();
        return Result.Failure(new Error("User.RoleAssignmentFailed", errorMessage ?? "Failed to assign role to user in Identity."));
    }

    public async Task<Result> UpdateEmailAsync(User user, string newEmail,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(user.Email);
        if (identityUser == null)
        {
            return Result.Failure(new Error("User.NotFound", "The specified user was not found in Identity."));
        }

        var emailResult = await userManager.SetEmailAsync(identityUser, newEmail);
        if (!emailResult.Succeeded)
        {
            var errorMessage = emailResult.Errors.Select(e => e.Description).FirstOrDefault();
            return Result.Failure(new Error("User.UpdateEmailFailed", errorMessage ?? "Failed to update email."));
        }

        var usernameResult = await userManager.SetUserNameAsync(identityUser, newEmail);
        if (!usernameResult.Succeeded)
        {
            var errorMessage = usernameResult.Errors.Select(e => e.Description).FirstOrDefault();
            return Result.Failure(new Error("User.UpdateUserNameFailed", errorMessage ?? "Failed to update username."));
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(user.Email);
        if (identityUser == null)
        {
            return Result.Failure(new Error("User.NotFound", "The specified user was not found in Identity."));
        }

        var result = await userManager.DeleteAsync(identityUser);
        if (!result.Succeeded)
        {
            var errorMessage = result.Errors.Select(e => e.Description).FirstOrDefault();
            return Result.Failure(new Error("User.DeleteFailed", errorMessage ?? "Failed to delete user."));
        }

        return Result.Success();
    }
}

