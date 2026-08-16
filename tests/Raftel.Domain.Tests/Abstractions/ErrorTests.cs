using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Abstractions;

public class ErrorTests
{
    [Fact]
    public void TwoArgConstruction_Should_DefaultToFailureType()
    {
        var error = new Error("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void Validation_Should_CreateErrorWithValidationType()
    {
        var error = Error.Validation("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_Should_CreateErrorWithNotFoundType()
    {
        var error = Error.NotFound("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_Should_CreateErrorWithConflictType()
    {
        var error = Error.Conflict("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_Should_CreateErrorWithUnauthorizedType()
    {
        var error = Error.Unauthorized("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_Should_CreateErrorWithForbiddenType()
    {
        var error = Error.Forbidden("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public void Failure_Should_CreateErrorWithFailureType()
    {
        var error = Error.Failure("Some.Code", "Some message");

        error.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void None_Should_BeEqualByValue_ToTwoArgErrorWithEmptyCodeAndMessage()
    {
        Error.None.ShouldBe(new Error(string.Empty, string.Empty));
    }
}
