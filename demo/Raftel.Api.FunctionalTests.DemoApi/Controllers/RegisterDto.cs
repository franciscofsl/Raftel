using System.ComponentModel.DataAnnotations;

namespace Raftel.Api.FunctionalTests.DemoApi.Controllers;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(8)]
    public string Password { get; set; } = null!;
}