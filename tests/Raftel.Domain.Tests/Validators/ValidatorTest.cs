using Raftel.Tests.Common.Domain;
using Shouldly;

namespace Raftel.Domain.Tests.Validators;

public class ValidatorTest
{
    [Fact]
    public void Validator_ShouldRetrieveFailures_IfFails()
    {
        var validator = new PirateValidator();

        var validationResult = validator.Validate(Mugiwara.Usopp);
        validationResult.IsValid.ShouldBeFalse();
        validationResult.Errors.ShouldContain(PirateErrors.LuffyShouldBeThePirateKing);
    }

    [Fact]
    public void Validator_ShouldRetrieveSuccess_IfNotFails()
    {
        var validator = new PirateValidator();

        var validationResult = validator.Validate(Mugiwara.Luffy);
        validationResult.IsValid.ShouldBeTrue();
        validationResult.Errors.ShouldBeEmpty();
    }
}