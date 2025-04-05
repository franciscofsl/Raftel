using Raftel.Core.Results;
using Shouldly;

namespace Raftel.Core.Tests.Results
{
    public class ResultTests
    {
        [Fact]
        public void Success_ShouldReturnSuccessResult()
        {
            var result = Result.Success();
            
            result.IsSuccess.ShouldBeTrue();
            result.Error.ShouldBe(string.Empty);
        }

        [Fact]
        public void Failure_ShouldReturnFailureResultWithErrorMessage()
        {
            var errorMessage = "An error occurred";

            var result = Result.Failure(errorMessage);

            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe(errorMessage);
        }
    }
}