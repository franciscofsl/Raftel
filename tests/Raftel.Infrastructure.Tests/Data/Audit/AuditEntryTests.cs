using Raftel.Infrastructure.Data.Audit;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditEntryTests
{
    [Fact]
    public void Create_ShouldCreateAuditEntryWithCorrectProperties()
    {
        var changeType = AuditChangeType.Create;
        var entityName = "TestEntity";
        var entityId = "123";
        var details = "Test details";

        var auditEntry = AuditEntry.Create(changeType, entityName, entityId, details);

        auditEntry.ChangeType.ShouldBe(changeType);
        auditEntry.EntityName.ShouldBe(entityName);
        auditEntry.EntityId.ShouldBe(entityId);
        auditEntry.Details.ShouldBe(details);
        auditEntry.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        auditEntry.PropertyChanges.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldCreateAuditEntryWithoutDetails()
    {
        var changeType = AuditChangeType.Update;
        var entityName = "TestEntity";
        var entityId = "456";

        var auditEntry = AuditEntry.Create(changeType, entityName, entityId);

        auditEntry.ChangeType.ShouldBe(changeType);
        auditEntry.EntityName.ShouldBe(entityName);
        auditEntry.EntityId.ShouldBe(entityId);
        auditEntry.Details.ShouldBeNull();
    }

    [Fact]
    public void AddPropertyChange_ShouldAddPropertyChangeToCollection()
    {
        var auditEntry = AuditEntry.Create(AuditChangeType.Update, "TestEntity", "123");
        
        auditEntry.AddPropertyChange("Name", "OldValue", "NewValue");

        auditEntry.PropertyChanges.Count.ShouldBe(1);
        var propertyChange = auditEntry.PropertyChanges.First();
        propertyChange.PropertyName.ShouldBe("Name");
        propertyChange.OldValue.ShouldBe("OldValue");
        propertyChange.NewValue.ShouldBe("NewValue");
        propertyChange.AuditEntryId.ShouldBe(auditEntry.Id);
    }

    [Fact]
    public void AddPropertyChange_ShouldAddMultiplePropertyChanges()
    {
        var auditEntry = AuditEntry.Create(AuditChangeType.Update, "TestEntity", "123");
        
        auditEntry.AddPropertyChange("Name", "OldName", "NewName");
        auditEntry.AddPropertyChange("Value", "10", "20");

        auditEntry.PropertyChanges.Count.ShouldBe(2);
        auditEntry.PropertyChanges.Any(pc => pc.PropertyName == "Name").ShouldBeTrue();
        auditEntry.PropertyChanges.Any(pc => pc.PropertyName == "Value").ShouldBeTrue();
    }
}