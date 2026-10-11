using Raftel.Application.Exceptions;
using Shouldly;

namespace Raftel.Application.UnitTests.Exceptions;

public class ConcurrencyConflictExceptionTests
{
    [Fact]
    public void Constructor_ShouldExposeEntityNames_AndNameThemInTheMessage()
    {
        var inner = new InvalidOperationException("inner");

        var exception = new ConcurrencyConflictException(["Tenant", "Role"], inner);

        exception.EntityNames.ShouldBe(["Tenant", "Role"]);
        exception.Message.ShouldContain("Tenant");
        exception.Message.ShouldContain("Role");
        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void Constructor_WithoutEntityNames_ShouldStillHaveAMessage()
    {
        var exception = new ConcurrencyConflictException([]);

        exception.EntityNames.ShouldBeEmpty();
        exception.Message.ShouldNotBeNullOrWhiteSpace();
    }
}
