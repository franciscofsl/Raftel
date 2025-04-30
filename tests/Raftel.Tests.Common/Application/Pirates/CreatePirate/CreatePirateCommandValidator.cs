using Raftel.Domain.Validators;

namespace Raftel.Tests.Common.Application.Pirates.CreatePirate;

public class CreatePirateCommandValidator : Validator<CreatePirateCommand>
{
    public CreatePirateCommandValidator()
    {
        EnsureThat(cmd => !string.IsNullOrWhiteSpace(cmd.Name), CreatePirateErrors.NameRequired);
        EnsureThat(cmd => !cmd.IsKing || cmd.Name == "Luffy", CreatePirateErrors.KingMustBeLuffy);
    }
}