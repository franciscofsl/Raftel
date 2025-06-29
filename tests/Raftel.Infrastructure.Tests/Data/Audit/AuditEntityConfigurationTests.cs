using Raftel.Infrastructure.Data.Audit;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditEntityConfigurationTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));

        configuration.EntityType.ShouldBe(typeof(TestEntity));
        configuration.AuditChildEntities.ShouldBeTrue();
        configuration.ExcludedProperties.ShouldBeEmpty();
        configuration.IncludedProperties.ShouldBeEmpty();
    }

    [Fact]
    public void ExcludeProperties_ShouldAddToExcludedProperties()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));

        configuration.ExcludeProperties("Property1", "Property2");

        configuration.ExcludedProperties.ShouldContain("Property1");
        configuration.ExcludedProperties.ShouldContain("Property2");
    }

    [Fact]
    public void IncludeOnlyProperties_ShouldAddToIncludedProperties()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));

        configuration.IncludeOnlyProperties("Property1", "Property2");

        configuration.IncludedProperties.ShouldContain("Property1");
        configuration.IncludedProperties.ShouldContain("Property2");
    }

    [Fact]
    public void ShouldAuditProperty_ShouldReturnFalse_WhenPropertyExcluded()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));
        configuration.ExcludeProperties("ExcludedProperty");

        var shouldAudit = configuration.ShouldAuditProperty("ExcludedProperty");

        shouldAudit.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAuditProperty_ShouldReturnTrue_WhenPropertyNotExcluded()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));
        configuration.ExcludeProperties("ExcludedProperty");

        var shouldAudit = configuration.ShouldAuditProperty("IncludedProperty");

        shouldAudit.ShouldBeTrue();
    }

    [Fact]
    public void ShouldAuditProperty_ShouldReturnTrue_WhenPropertyInIncludedList()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));
        configuration.IncludeOnlyProperties("IncludedProperty");

        var shouldAudit = configuration.ShouldAuditProperty("IncludedProperty");

        shouldAudit.ShouldBeTrue();
    }

    [Fact]
    public void ShouldAuditProperty_ShouldReturnFalse_WhenPropertyNotInIncludedList()
    {
        var configuration = new AuditEntityConfiguration(typeof(TestEntity));
        configuration.IncludeOnlyProperties("IncludedProperty");

        var shouldAudit = configuration.ShouldAuditProperty("ExcludedProperty");

        shouldAudit.ShouldBeFalse();
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}