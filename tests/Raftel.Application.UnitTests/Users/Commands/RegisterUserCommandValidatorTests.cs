using Raftel.Application.Features.Users.RegisterUser;
using Raftel.Domain.Features.Users.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Users.Commands;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _handler = new();

    [Fact]
    public void Validate_WhenEmailIsValid_ShouldReturnSuccess()
    {
        var command = new RegisterUserCommand("nami@onepiece.com", string.Empty);

        var result = _handler.Validate(command);

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
        var command = new RegisterUserCommand(invalidEmail, invalidEmail);

        var result = _handler.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == Email.InvalidFormatError.Code);
    }
}