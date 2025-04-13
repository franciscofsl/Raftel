using Raftel.Domain.Validators;

namespace Raftel.Domain.Tests.Common.Domain;

public sealed class PirateValidator : Validator<Pirate>
{
    public PirateValidator()
    {
        EnsureThat(_ => _.IsKing && _.Name == Mugiwara.Luffy.Name, PirateErrors.LuffyShouldBeThePirateKing);
    }
}