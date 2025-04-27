using Raftel.Shared.Extensions;
using Shouldly;

namespace Raftel.Shared.Tests.Extensions;

public class StringExtensionsTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("camelCase", "camelCase")]
    [InlineData("CamelCaseCase", "camelCaseCase")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    public void ToCamelCase_ShouldReturnExpectedResult(string input, string expected)
    {
        var result = input.ToCamelCase();
        result.ShouldBe(expected);
    }
}