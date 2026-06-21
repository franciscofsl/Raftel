using Raftel.Domain.Auditing;
using Shouldly;

namespace Raftel.Domain.Tests.Auditing;

public class EntityChangeTests
{
    [Fact]
    public void Create_ShouldSetAllProperties_AndStartWithNoAffectedProperties()
    {
        var timestamp = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var entityChange = EntityChange.Create(timestamp, "Raftel.Demo.Domain.Pirates.Pirate", AuditChangeType.Created, "pirate-id");

        entityChange.Timestamp.ShouldBe(timestamp);
        entityChange.EntityFullName.ShouldBe("Raftel.Demo.Domain.Pirates.Pirate");
        entityChange.ChangeType.ShouldBe(AuditChangeType.Created);
        entityChange.EntityId.ShouldBe("pirate-id");
        entityChange.AffectedProperties.ShouldBeEmpty();
    }

    [Fact]
    public void RegisterPropertyChange_ShouldAddToAffectedProperties()
    {
        var entityChange = EntityChange.Create(DateTime.UtcNow, "Pirate", AuditChangeType.Updated, "pirate-id");

        entityChange.RegisterPropertyChange("Bounty", "System.UInt32", "100", "200");

        entityChange.AffectedProperties.ShouldHaveSingleItem();
        var propertyChange = entityChange.AffectedProperties.Single();
        propertyChange.PropertyName.ShouldBe("Bounty");
        propertyChange.OldValue.ShouldBe("100");
        propertyChange.NewValue.ShouldBe("200");
    }

    [Fact]
    public void RegisterPropertyChange_CalledMultipleTimes_ShouldAccumulateAllChanges()
    {
        var entityChange = EntityChange.Create(DateTime.UtcNow, "Pirate", AuditChangeType.Updated, "pirate-id");

        entityChange.RegisterPropertyChange("Bounty", "System.UInt32", "100", "200");
        entityChange.RegisterPropertyChange("IsKing", "System.Boolean", "False", "True");

        entityChange.AffectedProperties.Count.ShouldBe(2);
    }
}
