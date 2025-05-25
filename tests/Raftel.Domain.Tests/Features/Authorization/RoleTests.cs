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
    public void AddPermission_ShouldAddSuccessfully()
    {
        var role = Role.Create("TestRole").Value;

        var result = role.AddPermission("Pirates.Navigate", "Navigate the seas");

        result.IsSuccess.ShouldBeTrue();
        role.HasPermission("Pirates.Navigate").ShouldBeTrue();
    }
