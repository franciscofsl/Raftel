using System.Security.Claims;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Users.LogInUser;

public sealed record LogInResult(ClaimsPrincipal ClaimsPrincipal, string Scheme);