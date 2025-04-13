using Raftel.Domain.Validators;

namespace Raftel.Application.Tests.Common;

public class CreatePirateCommandValidator : Validator<CreatePirateCommand>
{
    public CreatePirateCommandValidator()
    {
        EnsureThat(cmd => !string.IsNullOrWhiteSpace(cmd.Name), CreatePirateErrors.NameRequired);
        EnsureThat(cmd => !cmd.IsKing || cmd.Name == "Luffy", CreatePirateErrors.KingMustBeLuffy);
    }
}