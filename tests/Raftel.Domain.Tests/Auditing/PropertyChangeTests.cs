using Raftel.Domain.Auditing;
using Shouldly;

namespace Raftel.Domain.Tests.Auditing;

public class PropertyChangeTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var propertyChange = PropertyChange.Create("Bounty", "System.UInt32", "100", "200");

        propertyChange.PropertyName.ShouldBe("Bounty");
        propertyChange.PropertyType.ShouldBe("System.UInt32");
        propertyChange.OldValue.ShouldBe("100");
        propertyChange.NewValue.ShouldBe("200");
    }

    [Fact]
    public void Create_ShouldAllowNullOldAndNewValues()
    {
        var propertyChange = PropertyChange.Create("Bounty", "System.UInt32", null, null);

        propertyChange.OldValue.ShouldBeNull();
        propertyChange.NewValue.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldAssignUniqueId()
    {
        var first = PropertyChange.Create("Bounty", "System.UInt32", "100", "200");
        var second = PropertyChange.Create("Bounty", "System.UInt32", "100", "200");

        first.Id.ShouldNotBe(second.Id);
    }
}
