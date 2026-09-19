using Raftel.Infrastructure.Correlation;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Correlation;

public class CorrelationIdSanitizerTests
{
    [Theory]
    [InlineData("abc123")]
    [InlineData("abc-123_XYZ")]
    [InlineData("a")]
    public void IsValid_ShouldReturnTrue_ForAllowedCharacters(string value)
    {
        CorrelationIdSanitizer.IsValid(value).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_AtMaxLength()
    {
        var value = new string('a', 128);

        CorrelationIdSanitizer.IsValid(value).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenLongerThanMaxLength()
    {
        var value = new string('a', 129);

        CorrelationIdSanitizer.IsValid(value).ShouldBeFalse();
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has\nnewline")]
    [InlineData("has\rcarriagereturn")]
    [InlineData("has\ttab")]
    [InlineData("has.dot")]
    [InlineData("has/slash")]
    [InlineData("has;semicolon")]
    public void IsValid_ShouldReturnFalse_ForDisallowedCharacters(string value)
    {
        CorrelationIdSanitizer.IsValid(value).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNull()
    {
        CorrelationIdSanitizer.IsValid(null).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenEmpty()
    {
        CorrelationIdSanitizer.IsValid(string.Empty).ShouldBeFalse();
    }
}
