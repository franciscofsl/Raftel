using Raftel.Application.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions;

public class RequestEventTests
{
    private readonly RequestEvent _requestEvent = new();

    [Fact]
    public void Set_ShouldStoreField_WhenNameIsAllowed()
    {
        _requestEvent.Set("user.id", "user_123");

        _requestEvent.Fields["user.id"].ShouldBe("user_123");
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("user.password")]
    [InlineData("token")]
    [InlineData("access_token")]
    [InlineData("secret")]
    [InlineData("client_secret")]
    [InlineData("apikey")]
    [InlineData("api_apikey_value")]
    [InlineData("authorization")]
    [InlineData("Authorization")]
    public void Set_ShouldDropField_WhenNameMatchesDenyList(string field)
    {
        _requestEvent.Set(field, "should-not-be-stored");

        _requestEvent.Fields.ShouldNotContainKey(field);
    }

    [Fact]
    public void Increment_ShouldStartFromZero_WhenFieldIsNew()
    {
        _requestEvent.Increment("db.query_count");

        _requestEvent.Fields["db.query_count"].ShouldBe(1L);
    }

    [Fact]
    public void Increment_ShouldAccumulate_AcrossMultipleCalls()
    {
        _requestEvent.Increment("db.query_count");
        _requestEvent.Increment("db.query_count", 2);
        _requestEvent.Increment("db.query_count", 3);

        _requestEvent.Fields["db.query_count"].ShouldBe(6L);
    }

    [Fact]
    public void Increment_ShouldDropField_WhenNameMatchesDenyList()
    {
        _requestEvent.Increment("secret_count");

        _requestEvent.Fields.ShouldNotContainKey("secret_count");
    }
}
