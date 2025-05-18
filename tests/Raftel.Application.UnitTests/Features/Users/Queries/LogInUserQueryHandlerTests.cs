using System.Security.Claims;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.LogInUser;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.Queries;

public sealed class LogInUserQueryHandlerTests
{
    private readonly IAuthenticationService _authenticationService;
    private readonly LogInUserQueryHandler _handler;

    public LogInUserQueryHandlerTests()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _handler = new LogInUserQueryHandler(_authenticationService);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsAreValid_ShouldReturnSuccessWithClaimsPrincipal()
    {
        var query = new LogInUserQuery("luffy@onepiece.com", "GomuGomuNoBazooka123!");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "luffy"),
            new Claim(ClaimTypes.Email, "luffy@onepiece.com")
        }, "GomuGomuAuth"));

        var result = Result.Success(new LogInResult(claimsPrincipal, "GomuGomuAuth"));
        _authenticationService
            .LogInAsync(query.Email, query.Password, Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await _handler.HandleAsync(query);

        response.IsSuccess.ShouldBeTrue();
        response.Value.Claims.ShouldBe(claimsPrincipal);
        response.Value.Scheme.ShouldBe("GomuGomuAuth");
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsAreInvalid_ShouldReturnFailure()
    {
        var query = new LogInUserQuery("zoro@onepiece.com", "SantoryuIsLife");

        var error = new Error("INVALID_CREDENTIALS", "Lost credentials.");
        var result = Result.Failure<LogInResult>(error);

        _authenticationService
            .LogInAsync(query.Email, query.Password, Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await _handler.HandleAsync(query);

        response.IsSuccess.ShouldBeFalse();
        response.Error.ShouldBe(error);
    }
}