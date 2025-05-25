using Raftel.Domain.Features.Authorization;
using Shouldly;

namespace Raftel.Domain.Tests.Features.Authorization;

public class RoleTests
{
    [Fact]
    public void CreateAdministratorRole_ShouldCreateSuccessfully()
    {
        var result = Role.Create("Administrator", "Full system access");

        result.IsSuccess.ShouldBeTrue();
        var role = result.Value;
        role.Name.ShouldBe("Administrator");
        role.Description.ShouldBe("Full system access");
        role.Id.ShouldNotBe(default);
    }

    [Fact]
    public void CreateUserRole_WithoutDescription_ShouldCreateSuccessfully()
    {
        var result = Role.Create("User");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRole_WithInvalidName_ShouldFail(string invalidName)
    {
        var result = Role.Create(invalidName);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RoleErrors.InvalidName);
    }

    [Fact]
    public void Rename_ShouldUpdateSuccessfully()
    {
        var role = Role.Create("OldRoleName").Value;

        var result = role.Rename("NewRoleName");

        result.IsSuccess.ShouldBeTrue();
        role.Name.ShouldBe("NewRoleName");
    }
    
    [Fact]
    public void Rename_ShouldNotUpdate_WithInvalidName()
    {
        var role = Role.Create("OldRoleName").Value;

        var result = role.Rename(null);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(RoleErrors.InvalidName);
        role.Name.ShouldBe("OldRoleName");
    }

    [Fact]
    public void AddPermission_ShouldAddSuccessfully()
    {
        var role = Role.Create("TestRole").Value;

        var result = role.AddPermission("Pirates.Navigate", "Navigate the seas");

        result.IsSuccess.ShouldBeTrue();
        role.HasPermission("Pirates.Navigate").ShouldBeTrue();
    }

    [Fact]
    public void RemovePermission_ShouldRemoveSuccessfully()
    {
        var role = Role.Create("TestRole").Value;
        role.AddPermission("Temporary.Permission");

        var result = role.RemovePermission("Temporary.Permission");

        result.IsSuccess.ShouldBeTrue();
        role.HasPermission("Temporary.Permission").ShouldBeFalse();
    }

    [Fact]
    public void ClearPermissions_ShouldRemoveAll()
    {
        var role = Role.Create("TestRole").Value;
        role.AddPermission("Permission1");
        role.AddPermission("Permission2");
        role.AddPermission("Permission3");

        role.ClearPermissions();

        role.HasPermission("Permission1").ShouldBeFalse();
        role.HasPermission("Permission2").ShouldBeFalse();
        role.HasPermission("Permission3").ShouldBeFalse();
    }

    [Fact]
    public void AddDuplicatePermission_ShouldPreventIt()
    {
        var role = Role.Create("TestRole").Value;
        role.AddPermission("Users.Create");

        var result = role.AddPermission("Users.Create");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RoleErrors.PermissionAlreadyExists);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPermission_WithInvalidName_ShouldFail(string invalidName)
    {
        var role = Role.Create("TestRole").Value;

        var result = role.AddPermission(invalidName);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RoleErrors.InvalidPermissionName);
    }

    [Fact]
    public void RemoveNonExistentPermission_ShouldFail()
    {
        var role = Role.Create("TestRole").Value;

        var result = role.RemovePermission("NonExistent.Permission");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RoleErrors.PermissionNotFound);
    }

    [Theory]
    [InlineData("Users.Create", "USERS.CREATE", true)]
    [InlineData("Users.Read", "users.read", true)]
    [InlineData("Users.Update", "NonExistent.Permission", false)]
    public void QueryPermission_ShouldBeCaseInsensitive(string addedPermission, string queriedPermission,
        bool expectedResult)
    {
        var role = Role.Create("TestRole").Value;
        if (!string.IsNullOrEmpty(addedPermission))
        {
            role.AddPermission(addedPermission);
        }

        var hasPermission = role.HasPermission(queriedPermission);

        hasPermission.ShouldBe(expectedResult);
    }

    [Fact]
    public void AddMultiplePermissions_ShouldAddAll()
    {
        var role = Role.Create("TestRole").Value;
        var permissions = new[] { "Users.Create", "Users.Read", "Users.Update" };

        role.AddPermissions(permissions);

        role.HasPermission("Users.Create").ShouldBeTrue();
        role.HasPermission("Users.Read").ShouldBeTrue();
        role.HasPermission("Users.Update").ShouldBeTrue();
    }

    [Fact]
    public void AddMultiplePermissions_WithDuplicates_ShouldSkipDuplicates()
    {
        var role = Role.Create("TestRole").Value;
        role.AddPermission("Users.Create");

        var permissions = new[] { "Users.Create", "Users.Read" };

        role.AddPermissions(permissions);
    }

    [Fact]
    public void CreatePirateCaptainRole_WithFullPermissions_ShouldWork()
    {
        var captainRole = Role.Create("Captain", "Captain of the pirate ship").Value;

        captainRole.AddPermission("Ship.Navigate", "Navigate the ship");
        captainRole.AddPermission("Ship.Command", "Command the ship");

        captainRole.AddPermission("Crew.Manage", "Manage crew members");
        captainRole.AddPermission("Crew.Recruit", "Recruit new crew");

        captainRole.AddPermission("Treasure.Search", "Search for treasure");
        captainRole.AddPermission("Treasure.Distribute", "Distribute treasure");

        captainRole.AddPermission("Battle.Lead", "Lead in battle");
        captainRole.AddPermission("Bounty.Set", "Set bounties");

        captainRole.HasPermission("Ship.Navigate").ShouldBeTrue();
        captainRole.HasPermission("Crew.Manage").ShouldBeTrue();
        captainRole.HasPermission("Treasure.Distribute").ShouldBeTrue();
        captainRole.HasPermission("Battle.Lead").ShouldBeTrue();
    }

    [Fact]
    public void CreateSailorRole_WithLimitedPermissions_ShouldWork()
    {
        var sailorRole = Role.Create("Sailor", "Regular crew member").Value;

        sailorRole.AddPermission("Ship.Maintain", "Maintain the ship");
        sailorRole.AddPermission("Treasure.Search", "Help search for treasure");
        sailorRole.AddPermission("Battle.Fight", "Fight in battles");

        sailorRole.HasPermission("Ship.Maintain").ShouldBeTrue();
        sailorRole.HasPermission("Treasure.Search").ShouldBeTrue();
        sailorRole.HasPermission("Battle.Fight").ShouldBeTrue();

        sailorRole.HasPermission("Ship.Command").ShouldBeFalse();
        sailorRole.HasPermission("Crew.Manage").ShouldBeFalse();
        sailorRole.HasPermission("Treasure.Distribute").ShouldBeFalse();
    }

    [Fact]
    public void ComplexOperations_ShouldMaintainConsistentState()
    {
        var role = Role.Create("ComplexRole").Value;

        role.AddPermission("Perm1", "Description 1");
        role.AddPermission("Perm2", "Description 2");
        role.AddPermission("Perm3", "Description 3");

        role.Rename("UpdatedRole");
        role.Description = "Updated description";

        role.RemovePermission("Perm2");
        role.AddPermission("Perm4", "Description 4");

        role.ClearPermissions();
        role.AddPermission("FinalPerm", "Final permission");

        role.Name.ShouldBe("UpdatedRole");
        role.Description.ShouldBe("Updated description");
        role.HasPermission("FinalPerm").ShouldBeTrue();
        role.HasPermission("Perm1").ShouldBeFalse();
        role.HasPermission("Perm3").ShouldBeFalse();
    }
}