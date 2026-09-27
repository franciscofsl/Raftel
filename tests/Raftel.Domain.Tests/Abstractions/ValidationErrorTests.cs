using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Abstractions;

public class ValidationErrorTests
{
    [Fact]
    public void FromErrors_Should_AggregateEveryErrorWithoutLosingAny()
    {
        var errors = new[]
        {
            Error.Validation("Name.Required", "Name is required."),
            Error.Validation("Email.Invalid", "Email is not valid."),
            Error.Validation("Age.OutOfRange", "Age must be between 0 and 120.")
        };

        var validationError = ValidationError.FromErrors(errors);

        validationError.Errors.Count.ShouldBe(3);
        validationError.Errors.ShouldBe(errors);
    }

    [Fact]
    public void ValidationError_Should_HaveValidationErrorType()
    {
        var validationError = ValidationError.FromErrors([Error.Validation("Name.Required", "Name is required.")]);

        validationError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void TwoValidationErrors_WithSameAggregatedErrors_Should_BeEqual()
    {
        var errors = new[] { Error.Validation("Name.Required", "Name is required.") };

        var first = ValidationError.FromErrors(errors);
        var second = ValidationError.FromErrors(errors);

        first.ShouldBe(second);
    }

    [Fact]
    public void TwoValidationErrors_WithDifferentAggregatedErrors_Should_NotBeEqual()
    {
        var first = ValidationError.FromErrors([Error.Validation("Name.Required", "Name is required.")]);
        var second = ValidationError.FromErrors([Error.Validation("Email.Invalid", "Email is not valid.")]);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void ValidationError_Should_BeAssignableToError()
    {
        Error error = ValidationError.FromErrors([Error.Validation("Name.Required", "Name is required.")]);

        error.Type.ShouldBe(ErrorType.Validation);
    }
}
