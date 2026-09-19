using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Raftel.Infrastructure.Authentication;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Authentication;

public class CurrentHttpUserTests
{
    private const string PermissionClaimType = "permission";

    [Fact]
    public void HasPermission_WhenNoHttpContext_ShouldReturnFalseWithoutThrowing()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var currentUser = new CurrentHttpUser(accessor);

        var result = currentUser.HasPermission("pirates.manage");

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_WhenUserIsNotAuthenticated_ShouldReturnFalse()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var currentUser = new CurrentHttpUser(accessor);

        var result = currentUser.HasPermission("pirates.manage");

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_WhenAuthenticatedUserHasThePermission_ShouldReturnTrue()
    {
        var currentUser = CurrentUserWithPermissions("pirates.manage");

        currentUser.HasPermission("pirates.manage").ShouldBeTrue();
    }

    [Fact]
    public void HasPermission_WhenAuthenticatedUserLacksThePermission_ShouldReturnFalse()
    {
        var currentUser = CurrentUserWithPermissions("pirates.view");

        currentUser.HasPermission("pirates.manage").ShouldBeFalse();
    }

    private static CurrentHttpUser CurrentUserWithPermissions(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim(PermissionClaimType, p)).ToList();
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        return new CurrentHttpUser(accessor);
    }
}
