using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class AuthenticationTest : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public AuthenticationTest(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    private record RegisterRequest(string Email, string Password);

    private record RegisterResponse(string Message);

    private class LoginResponse
    {
        [JsonPropertyName("access_token")] public string Token { get; set; } = null!;

        [JsonPropertyName("expires_in")] public int Expiration { get; set; }
    }

    [Fact]
    public async Task Register_NewUser_ShouldReturnOk()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var password = "Password123!";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        data!.Message.ShouldBe("User successfully registered");
    }

    [Fact]
    public async Task Login_AfterRegistration_ShouldReturnToken()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var password = "Password123!";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        reg.EnsureSuccessStatusCode();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = "web-app",
            ["scope"] = "api offline_access"
        };
        var tokenResponse = await _client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(form)
        );

        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<LoginResponse>();
        tokenData!.Token.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_AfterRegistration_ShouldReturnToken_AndCallPerfil()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var password = "Password123!";
        var reg = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password)
        );
        reg.EnsureSuccessStatusCode();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = "web-app",
            ["scope"] = "api offline_access"
        };
        var tokenResponse = await _client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(form)
        );

        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<LoginResponse>();
        tokenData!.Token.ShouldNotBeNullOrEmpty();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.Token);

        var perfilResponse = await _client.GetAsync("/api/perfil");
        perfilResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var perfilJson = await perfilResponse.Content.ReadFromJsonAsync<PerfilResponse>();
        perfilJson!.Message.ShouldBe("Usuario autenticado");
    }

    private record PerfilResponse(
        [property: JsonPropertyName("message")]
        string Message,
        [property: JsonPropertyName("user")] string User
    );
}