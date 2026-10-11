using Raftel.Application.Abstractions.Concurrency;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions;

public class ExpectedVersionTests
{
    [Fact]
    public void Value_ByDefault_IsNull()
    {
        new ExpectedVersion().Value.ShouldBeNull();
    }

    [Fact]
    public void Expect_ThenConsume_ReturnsTheVersionOnce()
    {
        var expected = new ExpectedVersion();
        expected.Expect("abc");

        expected.Value.ShouldBe("abc");
        expected.Consume().ShouldBe("abc");
        expected.Consume().ShouldBeNull();
        expected.Value.ShouldBeNull();
    }

    [Fact]
    public void Expect_WithBlankVersion_Throws()
    {
        Should.Throw<ArgumentException>(() => new ExpectedVersion().Expect(" "));
    }
}
