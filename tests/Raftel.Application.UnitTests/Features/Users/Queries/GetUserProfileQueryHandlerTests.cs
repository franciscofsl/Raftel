using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Users.GetUserProfile;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.Queries;

public sealed class GetUserProfileQueryHandlerTests
{
    private readonly ICurrentUser _currentUser;
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _handler = new GetUserProfileQueryHandler(_currentUser);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAuthenticated_ShouldReturnProfile()
    {
        _currentUser.UserId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _currentUser.UserName.Returns("luffy");
        _currentUser.Roles.Returns(new[] { "Captain", "PirateKing" });
        _currentUser.IsAuthenticated.Returns(true);

        var query = new GetUserProfileQuery();

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserName.ShouldBe("luffy");
        result.Value.IsAuthenticated.ShouldBeTrue();
        result.Value.UserId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.Value.Roles.ShouldBe(new[] { "Captain", "PirateKing" });
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotAuthenticated_ShouldReturnEmptyUserId()
    {
        _currentUser.UserId.Returns((Guid?)null);
        _currentUser.UserName.Returns("brook");
        _currentUser.Roles.Returns(Array.Empty<string>());
        _currentUser.IsAuthenticated.Returns(false);

        var query = new GetUserProfileQuery();

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsAuthenticated.ShouldBeFalse();
        result.Value.UserId.ShouldBe(Guid.Empty);
        result.Value.UserName.ShouldBe("brook");
        result.Value.Roles.ShouldBeEmpty();
    }
}