using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.FunctionalTests.Extensions;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class SuccessStatusCodeTests
{
    private readonly HttpClient _client;

    public SuccessStatusCodeTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    private record RegisterRequest(string Email, string Password);

    [Fact]
    public async Task CommandWithoutResultValue_ShouldReturn204_WithEmptyBody()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest(email, "Password123!"));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandWithResultValueAndCreatedRouteName_ShouldReturn201_WithLocationHeader()
    {
        await _client.AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/pirates", new
        {
            Name = "Created At Route Test",
            Bounty = 100,
            IsKing = false
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var pirateId = await response.Content.ReadFromJsonAsync<Guid>();
        pirateId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CommandWithResultValueAndNoCreatedRouteName_ShouldReturn200_WithBody()
    {
        var response = await _client.PostAsJsonAsync("/api/test/resources", new { Name = "No Created Route" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resourceId = await response.Content.ReadFromJsonAsync<Guid>();
        resourceId.ShouldNotBe(Guid.Empty);
    }
}
