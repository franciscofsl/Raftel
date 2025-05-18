using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Raftel.Infrastructure.Authentication;

public interface IClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> CreateAsync(IdentityUser user);
}