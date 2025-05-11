using Raftel.Domain.Users;
using Raftel.Domain.Users.ValueObjects;
using Raftel.Domain.Validators;

namespace Raftel.Application.Users.CreateUser;

public sealed class CreateUserCommandValidator : Validator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        EnsureThat(_ => Email.IsEmail(_.Email), Email.InvalidFormatError);
    }
}