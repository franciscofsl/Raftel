using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Raftel.Application.Users.RegisterUser;

namespace Raftel.Api.FunctionalTests.DemoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ICommandDispatcher dispatcher) : ControllerBase
{ 
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await dispatcher.DispatchAsync(new RegisterUserCommand(dto.Email, dto.Email, dto.Email, dto.Password));

        if (result.IsSuccess)
        {
            return Ok(new { message = "User successfully registered" });
        }
 
        return BadRequest(new { message = "Cant register.", details = result.Error });
    }
}