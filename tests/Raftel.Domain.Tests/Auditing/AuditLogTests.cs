using Raftel.Domain.Auditing;
using Shouldly;

namespace Raftel.Domain.Tests.Auditing;

public class AuditLogTests
{
    [Fact]
    public void For_ShouldSetAllProperties_AndStartWithNoChanges()
    {
        var timestamp = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        var auditLog = AuditLog.For("CreatePirateCommand", timestamp, userId, "Luffy", "details");

        auditLog.Command.ShouldBe("CreatePirateCommand");
        auditLog.Timestamp.ShouldBe(timestamp);
        auditLog.UserId.ShouldBe(userId);
        auditLog.UserName.ShouldBe("Luffy");
        auditLog.Details.ShouldBe("details");
        auditLog.EntityChanges.ShouldBeEmpty();
        auditLog.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public void For_ShouldAllowNullUserAndDetails()
    {
        var auditLog = AuditLog.For("AnonymousCommand", DateTime.UtcNow);

        auditLog.UserId.ShouldBeNull();
        auditLog.UserName.ShouldBeNull();
        auditLog.Details.ShouldBeNull();
    }

    [Fact]
    public void RegisterChange_ShouldAddToEntityChanges_AndSetHasChanges()
    {
        var auditLog = AuditLog.For("CreatePirateCommand", DateTime.UtcNow);
        var entityChange = EntityChange.Create(DateTime.UtcNow, "Pirate", AuditChangeType.Created, "pirate-id");

        auditLog.RegisterChange(entityChange);

        auditLog.EntityChanges.ShouldHaveSingleItem();
        auditLog.EntityChanges.Single().ShouldBe(entityChange);
        auditLog.HasChanges.ShouldBeTrue();
    }

    [Fact]
    public void RegisterChange_CalledMultipleTimes_ShouldAccumulateAllEntityChanges()
    {
        var auditLog = AuditLog.For("CreatePirateCommand", DateTime.UtcNow);

        auditLog.RegisterChange(EntityChange.Create(DateTime.UtcNow, "Pirate", AuditChangeType.Created, "pirate-id"));
        auditLog.RegisterChange(EntityChange.Create(DateTime.UtcNow, "Ship", AuditChangeType.Created, "ship-id"));

        auditLog.EntityChanges.Count.ShouldBe(2);
    }
}
