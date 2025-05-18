using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.RegisterUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.Commands;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(_authenticationService, _usersRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenEmailIsNotUnique()
    {
        var command = new RegisterUserCommand("luffy@onepiece.com", "GomuGomuNoMi");
        _usersRepository.EmailIsUniqueAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.DuplicatedEmail);
        await _authenticationService.DidNotReceiveWithAnyArgs().RegisterAsync(default!, default!, default);
        await _usersRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenRegistrationFails()
    {
        var command = new RegisterUserCommand("zoro@onepiece.com", "Santoryu");
        _usersRepository.EmailIsUniqueAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        _authenticationService.RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Registration failed", "Registration failed")));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _usersRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_And_AddUser_WhenRegistrationSucceeds()
    {
        var command = new RegisterUserCommand("nami@onepiece.com", "Navigation123");
        _usersRepository.EmailIsUniqueAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        _authenticationService.RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _usersRepository.Received(1)
            .AddAsync(Arg.Is<User>(u => u.Email == command.Email), Arg.Any<CancellationToken>());
    }
}