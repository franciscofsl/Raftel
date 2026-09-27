using Microsoft.AspNetCore.Http.HttpResults;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Api.FunctionalTests.AutoEndpoints;

public class ErrorResultsTests
{
    [Fact]
    public void ToProblem_ForValidationError_ShouldGroupErrorsByFieldName()
    {
        var validationError = ValidationError.FromErrors(
        [
            Error.Validation("Name.Required", "Name is required."),
            Error.Validation("Email.Invalid", "Email is not valid.")
        ]);

        var problem = (ProblemHttpResult)ErrorResults.ToProblem(validationError);

        var errors = (Dictionary<string, string[]>)problem.ProblemDetails.Extensions["errors"]!;
        errors["Name"].ShouldBe(["Name is required."]);
        errors["Email"].ShouldBe(["Email is not valid."]);
    }

    [Fact]
    public void ToProblem_ForValidationError_ShouldGroupMultipleErrorsForSameField()
    {
        var validationError = ValidationError.FromErrors(
        [
            Error.Validation("Name.Required", "Name is required."),
            Error.Validation("Name.TooShort", "Name is too short.")
        ]);

        var problem = (ProblemHttpResult)ErrorResults.ToProblem(validationError);

        var errors = (Dictionary<string, string[]>)problem.ProblemDetails.Extensions["errors"]!;
        errors["Name"].ShouldBe(["Name is required.", "Name is too short."]);
    }

    [Fact]
    public void ToProblem_ForValidationError_WhenCodeHasNoDot_ShouldGroupUnderEmptyFieldKey()
    {
        var validationError = ValidationError.FromErrors([Error.Validation("NameRequired", "Name is required.")]);

        var problem = (ProblemHttpResult)ErrorResults.ToProblem(validationError);

        var errors = (Dictionary<string, string[]>)problem.ProblemDetails.Extensions["errors"]!;
        errors[string.Empty].ShouldBe(["Name is required."]);
    }

    [Fact]
    public void ToProblem_ForNonValidationError_ShouldNotIncludeErrorsExtension()
    {
        var error = Error.NotFound("Test.NotFound", "Resource not found.");

        var problem = (ProblemHttpResult)ErrorResults.ToProblem(error);

        problem.ProblemDetails.Extensions.ContainsKey("errors").ShouldBeFalse();
    }
}
