using Raftel.Core.Results;
using Shouldly;

namespace Raftel.Core.Tests.Results;

public class ResultTTests
{
    [Fact]
    public void Success_WithValue_ShouldReturnSuccessResultWithValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBe(string.Empty);
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Failure_WithError_ShouldReturnFailureResultWithErrorMessage()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = Result<int>.Failure(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(errorMessage);
        result.Value.ShouldBe(0);  // Default value for int
    }
}