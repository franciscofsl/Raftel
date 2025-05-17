using Microsoft.AspNetCore.Identity;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;

namespace Raftel.Infrastructure.Authentication;

internal sealed class AuthenticationService(UserManager<IdentityUser> userManager) : IAuthenticationService
{
    public async Task<Result> RegisterAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var identityUser = new IdentityUser
        {
            UserName = user.Email,
            Email = user.Email
        };
        var result = await userManager.CreateAsync(identityUser, password);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errorMessage = result.Errors.Select(e => e.Description).FirstOrDefault();
        return Result.Failure(new Error("User.CantRegister", errorMessage));
    }
}