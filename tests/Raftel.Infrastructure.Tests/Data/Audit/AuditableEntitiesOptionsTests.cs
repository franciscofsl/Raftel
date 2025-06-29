using Raftel.Infrastructure.Data.Audit;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditableEntitiesOptionsTests
{
    [Fact]
    public void Add_ShouldRegisterEntityType()
    {
        var options = new AuditableEntitiesOptions();

        var configuration = options.Add<TestEntity>();

        configuration.ShouldNotBeNull();
        configuration.EntityType.ShouldBe(typeof(TestEntity));
        options.IsAuditable(typeof(TestEntity)).ShouldBeTrue();
    }

    [Fact]
    public void IsAuditable_ShouldReturnFalse_WhenEntityNotRegistered()
    {
        var options = new AuditableEntitiesOptions();

        var isAuditable = options.IsAuditable(typeof(TestEntity));

        isAuditable.ShouldBeFalse();
    }

    [Fact]
    public void GetConfiguration_ShouldReturnConfiguration_WhenEntityRegistered()
    {
        var options = new AuditableEntitiesOptions();
        options.Add<TestEntity>();

        var configuration = options.GetConfiguration(typeof(TestEntity));

        configuration.ShouldNotBeNull();
        configuration.EntityType.ShouldBe(typeof(TestEntity));
    }

    [Fact]
    public void GetConfiguration_ShouldReturnNull_WhenEntityNotRegistered()
    {
        var options = new AuditableEntitiesOptions();

        var configuration = options.GetConfiguration(typeof(TestEntity));

        configuration.ShouldBeNull();
    }

    [Fact]
    public void GetAuditableTypes_ShouldReturnAllRegisteredTypes()
    {
        var options = new AuditableEntitiesOptions();
        options.Add<TestEntity>();
        options.Add<AnotherTestEntity>();

        var types = options.GetAuditableTypes().ToList();

        types.Count.ShouldBe(2);
        types.ShouldContain(typeof(TestEntity));
        types.ShouldContain(typeof(AnotherTestEntity));
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherTestEntity
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}