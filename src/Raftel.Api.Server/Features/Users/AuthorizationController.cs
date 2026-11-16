using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Raftel.Api.Server.Security;
using Raftel.Application.Features.Users.LogInUser;
using Raftel.Application.Queries;

namespace Raftel.Api.Server.Features.Users;

[ApiController]
public class AuthorizationController(IQueryDispatcher dispatcher) : Controller
{
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    [AllowAnonymous]
    [EnableRateLimiting(RaftelRateLimitPolicies.Auth)]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;
        var query = new LogInUserQuery(request.Username, request.Password);
        var result = await dispatcher.DispatchAsync<LogInUserQuery, LogInUserResponse>(query);

        if (result.IsSuccess)
        {
            return SignIn(result.Value.Claims, result.Value.Scheme);
        }

        return BadRequest(result.Error);
    }
}