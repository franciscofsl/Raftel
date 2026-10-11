using Raftel.Domain.Features.Authorization;
using Shouldly;

namespace Raftel.Domain.Tests.Features.Authorization;

public class PermissionDefinitionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithBlankName_ShouldFail(string name)
    {
        var result = PermissionDefinition.Create(name);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RoleErrors.InvalidPermissionName);
    }

    [Fact]
    public void Create_ShouldStartActive()
    {
        var definition = PermissionDefinition.Create("Users.View", "View users").Value;

        definition.Name.ShouldBe("Users.View");
        definition.Description.ShouldBe("View users");
        definition.IsObsolete.ShouldBeFalse();
    }

    [Fact]
    public void MarkObsolete_ShouldFlagOnce()
    {
        var definition = PermissionDefinition.Create("Users.View").Value;

        definition.MarkObsolete().ShouldBeTrue();
        definition.IsObsolete.ShouldBeTrue();
        definition.MarkObsolete().ShouldBeFalse();
    }

    [Fact]
    public void Reinstate_ShouldClearTheFlagOnlyWhenObsolete()
    {
        var definition = PermissionDefinition.Create("Users.View").Value;

        definition.Reinstate().ShouldBeFalse();

        definition.MarkObsolete();
        definition.Reinstate().ShouldBeTrue();
        definition.IsObsolete.ShouldBeFalse();
    }
}
