using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    private record RegisterRequest(string Email, string Password);

    private class LoginResponse
    {
        [JsonPropertyName("access_token")] public string Token { get; set; } = null!;
    }

    public HealthCheckTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        const string password = "Password123!";

        var reg = await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest(email, password));
        reg.EnsureSuccessStatusCode();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = "web-app",
            ["scope"] = "api offline_access"
        };
        var tokenResponse = await _client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return tokenData!.Token;
    }

    private async Task ToggleDatabaseFailureAsync(bool enabled)
    {
        var path = enabled ? "/api/test/database-failure/enable" : "/api/test/database-failure/disable";
        var response = await _client.PostAsync(path, content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Live_ShouldReturn200_WhenDatabaseIsHealthy()
    {
        await ToggleDatabaseFailureAsync(false);

        var response = await _client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Live_ShouldReturn200_EvenWhenDatabaseIsFailing()
    {
        await ToggleDatabaseFailureAsync(true);
        try
        {
            var response = await _client.GetAsync("/health/live");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            await ToggleDatabaseFailureAsync(false);
        }
    }

    [Fact]
    public async Task Ready_ShouldReturn200_WhenHealthy()
    {
        await ToggleDatabaseFailureAsync(false);

        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_ShouldReturn503_WhenDatabaseIsFailing()
    {
        await ToggleDatabaseFailureAsync(true);
        try
        {
            var response = await _client.GetAsync("/health/ready");

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            await ToggleDatabaseFailureAsync(false);
        }
    }

    [Fact]
    public async Task Detailed_ShouldReturn401_WhenAnonymous()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("database");
        body.ShouldNotContain("Healthy");
    }

    [Fact]
    public async Task Detailed_ShouldReturnBody_WhenAuthorized()
    {
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.TryGetProperty("entries", out var entries).ShouldBeTrue();
        entries.TryGetProperty("database", out _).ShouldBeTrue();

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Live_And_Ready_ShouldStayAnonymous_EvenWithABearerSchemeConfigured()
    {
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");

        liveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        readyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_ShouldNotContainSensitiveDetail_WhenDatabaseFails()
    {
        await ToggleDatabaseFailureAsync(true);
        try
        {
            var response = await _client.GetAsync("/health/ready");
            var body = await response.Content.ReadAsStringAsync();

            body.ShouldNotContain("Exception");
            body.ShouldNotContain("Password");
            body.ShouldNotContain("Server=");
        }
        finally
        {
            await ToggleDatabaseFailureAsync(false);
        }
    }
}
