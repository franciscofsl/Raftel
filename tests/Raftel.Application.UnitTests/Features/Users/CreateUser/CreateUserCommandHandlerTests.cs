using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.CreateUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Users;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.CreateUser;

public sealed class CreateUserCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_authenticationService, _usersRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenEmailIsNotUnique()
    {
        var command = new CreateUserCommand("luffy@onepiece.com", "Luffy", "Monkey D.", "GomuGomuNoMi");
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
        var command = new CreateUserCommand("zoro@onepiece.com", "Zoro", "Roronoa", "Santoryu");
        _usersRepository.EmailIsUniqueAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        _authenticationService.RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Registration failed", "Registration failed")));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _usersRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_And_AddUser_WhenRegistrationSucceeds()
    {
        var command = new CreateUserCommand("nami@onepiece.com", "Nami", "Cat Burglar", "Navigation123");
        _usersRepository.EmailIsUniqueAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        _authenticationService.RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success("UserId123"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _usersRepository.Received(1)
            .AddAsync(Arg.Is<User>(u => u.Email == command.Email && u.Name == command.Name && u.Surname == command.Surname), Arg.Any<CancellationToken>());
    }
}
