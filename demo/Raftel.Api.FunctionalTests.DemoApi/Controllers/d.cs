using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Raftel.Api.FunctionalTests.DemoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AuthController(UserManager<IdentityUser> userManager)
        => _userManager = userManager;

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 1. Verificar si existe
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return Conflict(new { message = "El email ya está registrado." });

        // 2. Crear usuario
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "No se pudo registrar.", details = errors });
        }

        // 3. Registro exitoso
        return Ok(new { message = "Usuario registrado exitosamente." });
    }
}