using Raftel.Infrastructure.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Auditing;

public class AuditLogScopeTests
{
    [Fact]
    public void Command_WithNoActiveScope_ShouldBeNull()
    {
        var scope = new AuditLogScope();

        scope.Command.ShouldBeNull();
    }

    [Fact]
    public void Begin_ShouldSetCommand()
    {
        var scope = new AuditLogScope();

        using (scope.Begin("CreatePirateCommand"))
        {
            scope.Command.ShouldBe("CreatePirateCommand");
        }
    }

    [Fact]
    public void Dispose_ShouldRevertToPreviousCommand()
    {
        var scope = new AuditLogScope();

        using (scope.Begin("OuterCommand"))
        {
            using (scope.Begin("InnerCommand"))
            {
                scope.Command.ShouldBe("InnerCommand");
            }

            scope.Command.ShouldBe("OuterCommand");
        }

        scope.Command.ShouldBeNull();
    }
}
