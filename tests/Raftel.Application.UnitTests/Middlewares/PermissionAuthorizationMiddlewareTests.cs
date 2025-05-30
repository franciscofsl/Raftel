using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Authorization;
using Raftel.Application.Exceptions;
using Raftel.Application.Middlewares;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class PermissionAuthorizationMiddlewareTests
{
    private readonly ICurrentUser _currentUser;
    private readonly RequestHandlerDelegate<string> _next;
    private readonly PermissionAuthorizationMiddleware<CommandWithoutPermission, string> _middlewareWithoutPermission;
    private readonly PermissionAuthorizationMiddleware<CommandWithPermission, string> _middlewareWithPermission;
    private readonly PermissionAuthorizationMiddleware<CommandWithMultiplePermissions, string> _middlewareWithMultiplePermissions;

    public PermissionAuthorizationMiddlewareTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _next = () => Task.FromResult("Result");

        _middlewareWithoutPermission = new PermissionAuthorizationMiddleware<CommandWithoutPermission, string>(_currentUser);
        _middlewareWithPermission = new PermissionAuthorizationMiddleware<CommandWithPermission, string>(_currentUser);
        _middlewareWithMultiplePermissions = new PermissionAuthorizationMiddleware<CommandWithMultiplePermissions, string>(_currentUser);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasNoPermissionRequirements_ShouldAllowAccess()
    {
        _currentUser.IsAuthenticated.Returns(false);

        var result = await _middlewareWithoutPermission.HandleAsync(new CommandWithoutPermission(), _next);

        result.ShouldBe("Result");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasPermissionButUserIsNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        _currentUser.When(c => c.EnsureHasPermission("test.permission"))
            .Do(_ => throw new UnauthorizedException("User is not authenticated"));

        await Should.ThrowAsync<UnauthorizedException>(
            async () => await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasPermissionAndUserIsAuthenticatedButLacksPermission_ShouldThrowUnauthorizedException()
    {
        _currentUser.When(c => c.EnsureHasPermission("test.permission"))
            .Do(_ => throw new UnauthorizedException("User does not have the required permission: test.permission"));

        await Should.ThrowAsync<UnauthorizedException>(
            async () => await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasPermissionAndUserIsAuthenticatedAndHasPermission_ShouldAllowAccess()
    {
        var result = await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next);

        result.ShouldBe("Result");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasMultiplePermissionsAndUserHasAllPermissions_ShouldAllowAccess()
    {
        var result = await _middlewareWithMultiplePermissions.HandleAsync(new CommandWithMultiplePermissions(), _next);

        result.ShouldBe("Result");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasMultiplePermissionsAndUserLacksSomePermission_ShouldThrowUnauthorizedException()
    {
        _currentUser.When(c => c.EnsureHasPermission("test.permission1"))
            .Do(_ => { });
        
        _currentUser.When(c => c.EnsureHasPermission("test.permission2"))
            .Do(_ => throw new UnauthorizedException("User does not have the required permission: test.permission2"));

        await Should.ThrowAsync<UnauthorizedException>(
            async () => await _middlewareWithMultiplePermissions.HandleAsync(new CommandWithMultiplePermissions(), _next)
        );
    }

    private class CommandWithoutPermission : IRequest<string>
    {
    }

    [RequiresPermission("test.permission")]
    private class CommandWithPermission : IRequest<string>
    {
    }

    [RequiresPermission("test.permission1")]
    [RequiresPermission("test.permission2")]
    private class CommandWithMultiplePermissions : IRequest<string>
    {
    }
}
