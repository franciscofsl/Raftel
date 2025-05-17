using Raftel.Domain.Features.Users.ValueObjects;
using Raftel.Domain.Validators;

namespace Raftel.Application.Features.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : Validator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        EnsureThat(_ => Email.IsEmail(_.Email), Email.InvalidFormatError);
    }
}