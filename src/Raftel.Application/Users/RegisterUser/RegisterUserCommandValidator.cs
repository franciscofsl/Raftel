using Raftel.Domain.Users.ValueObjects;
using Raftel.Domain.Validators;

namespace Raftel.Application.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : Validator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        EnsureThat(_ => Email.IsEmail(_.Email), Email.InvalidFormatError);
    }
}