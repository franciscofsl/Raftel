using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Domain.Tests.Validators;

public class ValidatorTest
{
    [Fact]
    public void Validator_ShouldRetrieveFailures_IfFails()
    {
        var validator = new PirateValidator();

        var validationResult = validator.Validate(MugiwaraCrew.Usopp());
        validationResult.IsValid.ShouldBeFalse();
        validationResult.Errors.ShouldContain(PirateErrors.LuffyShouldBeThePirateKing);
    }

    [Fact]
    public void Validator_ShouldRetrieveSuccess_IfNotFails()
    {
        var validator = new PirateValidator();
        
        var luffy = MugiwaraCrew.Luffy();
        luffy.FoundOnePiece();
        
        var validationResult = validator.Validate(luffy);
        validationResult.IsValid.ShouldBeTrue();
        validationResult.Errors.ShouldBeEmpty();
    }
}