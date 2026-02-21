using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.DeleteUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.DeleteUser;

public sealed class DeleteUserCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _handler = new DeleteUserCommandHandler(_authenticationService, _usersRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId);
        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User)null);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenDeleteFromIdentityFails()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("luffy@onepiece.com", "Luffy", "Monkey D.");
        var command = new DeleteUserCommand(userId);

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var error = new Error("User.DeleteFailed", "Failed to delete user.");
        _authenticationService.DeleteAsync(user, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
        _usersRepository.DidNotReceive().Remove(user);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_And_RemoveUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("luffy@onepiece.com", "Luffy", "Monkey D.");
        var command = new DeleteUserCommand(userId);

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _authenticationService.DeleteAsync(user, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _usersRepository.Received(1).Remove(user);
    }
}
