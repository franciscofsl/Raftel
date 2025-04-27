using Raftel.Shared.Extensions;
using Shouldly;

namespace Raftel.Shared.Tests.Extensions;

public class TypeExtensionsTest
{
    [Fact]
    public void IsNullable_ReturnsTrue_WhenTypeIsNullable()
    {
        typeof(int?).IsNullable().ShouldBeTrue();
    }

    [Fact]
    public void IsNullable_ReturnsFalse_WhenTypeIsNonNullableValueType()
    {
        typeof(int).IsNullable().ShouldBeFalse();
    }

    [Fact]
    public void IsNullable_ReturnsTrue_WhenTypeIsReferenceType()
    {
        typeof(string).IsNullable().ShouldBeTrue();
    }
}