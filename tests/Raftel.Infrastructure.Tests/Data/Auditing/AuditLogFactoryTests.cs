using Raftel.Infrastructure.Data.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Auditing;

public class AuditLogFactoryTests
{
    private readonly AuditLogFactory _factory = new();

    [Fact]
    public void Create_WithNoEntityChanges_ShouldReturnNull()
    {
        var auditLog = _factory.Create("SomeCommand", DateTime.UtcNow, null, null, []);

        auditLog.ShouldBeNull();
    }

    [Fact]
    public void Create_WithSingleSnapshot_ShouldBuildAuditLogWithMatchingMetadata()
    {
        var timestamp = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        var snapshot = new EntityChangeSnapshot(
            "Raftel.Demo.Domain.Pirates.Pirate",
            "Created",
            "pirate-id",
            [new PropertyChangeSnapshot("Bounty", "System.UInt32", null, "100")]);

        var auditLog = _factory.Create("CreatePirateCommand", timestamp, userId, "Luffy", [snapshot]);

        auditLog.ShouldNotBeNull();
        auditLog.Command.ShouldBe("CreatePirateCommand");
        auditLog.Timestamp.ShouldBe(timestamp);
        auditLog.UserId.ShouldBe(userId);
        auditLog.UserName.ShouldBe("Luffy");
        auditLog.EntityChanges.ShouldHaveSingleItem();

        var entityChange = auditLog.EntityChanges.Single();
        entityChange.EntityFullName.ShouldBe("Raftel.Demo.Domain.Pirates.Pirate");
        entityChange.ChangeType.ShouldBe("Created");
        entityChange.EntityId.ShouldBe("pirate-id");
        entityChange.AffectedProperties.ShouldHaveSingleItem();

        var propertyChange = entityChange.AffectedProperties.Single();
        propertyChange.PropertyName.ShouldBe("Bounty");
        propertyChange.PropertyType.ShouldBe("System.UInt32");
        propertyChange.OldValue.ShouldBeNull();
        propertyChange.NewValue.ShouldBe("100");
    }

    [Fact]
    public void Create_WithMultipleSnapshots_ShouldProduceOneEntityChangePerSnapshot()
    {
        var snapshots = new[]
        {
            new EntityChangeSnapshot("Pirate", "Created", "pirate-id", []),
            new EntityChangeSnapshot("Ship", "Created", "ship-id", [])
        };

        var auditLog = _factory.Create("SomeCommand", DateTime.UtcNow, null, null, snapshots);

        auditLog.ShouldNotBeNull();
        auditLog.EntityChanges.Count.ShouldBe(2);
        auditLog.EntityChanges.Select(c => c.EntityFullName).ShouldBe(["Pirate", "Ship"]);
    }
}
