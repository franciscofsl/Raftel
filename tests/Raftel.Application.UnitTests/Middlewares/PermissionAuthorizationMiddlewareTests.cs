using NSubstitute;
using Raftel.Application;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Authorization;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class PermissionAuthorizationMiddlewareTests
{
    private readonly ICurrentUser _currentUser;
    private readonly AuthorizationOptions _authorizationOptions;
    private readonly RequestHandlerDelegate<Result> _next;
    private readonly PermissionAuthorizationMiddleware<CommandWithoutPermission, Result> _middlewareWithoutPermission;
    private readonly PermissionAuthorizationMiddleware<CommandWithPermission, Result> _middlewareWithPermission;
    private readonly PermissionAuthorizationMiddleware<CommandWithMultiplePermissions, Result> _middlewareWithMultiplePermissions;

    public PermissionAuthorizationMiddlewareTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _authorizationOptions = new AuthorizationOptions();
        _next = _ => Task.FromResult(Result.Success());

        _middlewareWithoutPermission =
            new PermissionAuthorizationMiddleware<CommandWithoutPermission, Result>(_currentUser, _authorizationOptions);
        _middlewareWithPermission =
            new PermissionAuthorizationMiddleware<CommandWithPermission, Result>(_currentUser, _authorizationOptions);
        _middlewareWithMultiplePermissions =
            new PermissionAuthorizationMiddleware<CommandWithMultiplePermissions, Result>(_currentUser, _authorizationOptions);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasNoPermissionRequirements_ShouldAllowAccess()
    {
        var result = await _middlewareWithoutPermission.HandleAsync(new CommandWithoutPermission(), _next, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserLacksTheRequiredPermission_ShouldReturnForbiddenResultWithoutInvokingNext()
    {
        _currentUser.HasPermission("test.permission").Returns(false);
        var nextInvoked = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult(Result.Success());
        };

        var result = await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), next, CancellationToken.None);

        nextInvoked.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasTheRequiredPermission_ShouldAllowAccess()
    {
        _currentUser.HasPermission("test.permission").Returns(true);

        var result = await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasAllRequiredPermissions_ShouldAllowAccess()
    {
        _currentUser.HasPermission("test.permission1").Returns(true);
        _currentUser.HasPermission("test.permission2").Returns(true);

        var result = await _middlewareWithMultiplePermissions.HandleAsync(new CommandWithMultiplePermissions(), _next, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserLacksSomeRequiredPermission_ShouldReturnForbiddenResult()
    {
        _currentUser.HasPermission("test.permission1").Returns(true);
        _currentUser.HasPermission("test.permission2").Returns(false);

        var result = await _middlewareWithMultiplePermissions.HandleAsync(new CommandWithMultiplePermissions(), _next, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeMissingPermissionsInErrorIsFalse_ShouldNotListMissingPermissions()
    {
        _authorizationOptions.IncludeMissingPermissionsInError = false;
        _currentUser.HasPermission("test.permission").Returns(false);

        var result = await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next, CancellationToken.None);

        result.Error.Message.ShouldNotContain("test.permission");
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeMissingPermissionsInErrorIsTrue_ShouldListMissingPermissions()
    {
        _authorizationOptions.IncludeMissingPermissionsInError = true;
        _currentUser.HasPermission("test.permission").Returns(false);

        var result = await _middlewareWithPermission.HandleAsync(new CommandWithPermission(), _next, CancellationToken.None);

        result.Error.Message.ShouldContain("test.permission");
    }

    private class CommandWithoutPermission : IRequest<Result>
    {
    }

    [RequiresPermission("test.permission")]
    private class CommandWithPermission : IRequest<Result>
    {
    }

    [RequiresPermission("test.permission1")]
    [RequiresPermission("test.permission2")]
    private class CommandWithMultiplePermissions : IRequest<Result>
    {
    }
}
