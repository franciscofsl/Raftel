using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.EditUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.EditUser;

public sealed class EditUserCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly EditUserCommandHandler _handler;

    public EditUserCommandHandlerTests()
    {
        _handler = new EditUserCommandHandler(_authenticationService, _usersRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var command = new EditUserCommand(userId, "Luffy", "Monkey D.", "luffy@onepiece.com");
        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User)null);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenNewEmailIsNotUnique()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("old@onepiece.com", "Luffy", "Monkey D.");
        var command = new EditUserCommand(userId, "Luffy", "Monkey D.", "new@onepiece.com");

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _usersRepository.EmailIsUniqueAsync("new@onepiece.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.DuplicatedEmail);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_WhenEmailUnchanged()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("luffy@onepiece.com", "Luffy", "Monkey D.");
        var command = new EditUserCommand(userId, "Updated", "Name", "luffy@onepiece.com");

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _usersRepository.Received(1).Update(user);
        await _authenticationService.DidNotReceiveWithAnyArgs()
            .UpdateEmailAsync(default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateEmailInIdentity_WhenEmailChanged()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("old@onepiece.com", "Luffy", "Monkey D.");
        var command = new EditUserCommand(userId, "Luffy", "Monkey D.", "new@onepiece.com");

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _usersRepository.EmailIsUniqueAsync("new@onepiece.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _authenticationService.UpdateEmailAsync(user, "new@onepiece.com", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _usersRepository.Received(1).Update(user);
        await _authenticationService.Received(1)
            .UpdateEmailAsync(user, "new@onepiece.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenUpdateEmailFails()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("old@onepiece.com", "Luffy", "Monkey D.");
        var command = new EditUserCommand(userId, "Luffy", "Monkey D.", "new@onepiece.com");

        _usersRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _usersRepository.EmailIsUniqueAsync("new@onepiece.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var error = new Error("User.UpdateEmailFailed", "Failed to update email.");
        _authenticationService.UpdateEmailAsync(user, "new@onepiece.com", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }
}
