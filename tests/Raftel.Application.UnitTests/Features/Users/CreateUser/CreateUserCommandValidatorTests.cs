using Raftel.Application.Features.Users.CreateUser;
using Raftel.Domain.Features.Users.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.CreateUser;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailIsValid_ShouldReturnSuccess()
    {
        var command = new CreateUserCommand("nami@onepiece.com", "Nami", "Cat Burglar", "password");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("zoro")]
    [InlineData("sanji@")]
    [InlineData("usopp@invalid")]
    [InlineData("chopper@.com")]
    public void Validate_WhenEmailIsInvalid_ShouldReturnFailure(string invalidEmail)
    {
        var command = new CreateUserCommand(invalidEmail, "Test", "User", "password");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == Email.InvalidFormatError.Code);
    }
}
