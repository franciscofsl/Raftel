using System.Security.Claims;

namespace Raftel.Application.Features.Users.LogInUser;

public sealed record LogInUserResponse(ClaimsPrincipal Claims, string Scheme);