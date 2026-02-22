using Raftel.Domain.Features.Users.ValueObjects;
using Raftel.Domain.Validators;

namespace Raftel.Application.Features.Users.CreateUser;

public sealed class CreateUserCommandValidator : Validator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        EnsureThat(_ => Email.IsEmail(_.Email), Email.InvalidFormatError);
    }
}
