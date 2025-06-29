using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Raftel.Api.FunctionalTests.Extensions;

public static class HttpClientExtensions
{
    public static async Task<string> AuthenticateAsync(this HttpClient client, string? email = null,
        string? password = null)
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        password ??= "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/api/users/register", new
        {
            Email = email,
            Password = password
        });
        registerResponse.EnsureSuccessStatusCode();

        return await client.AuthenticateWithExistingUserAsync(email, password);
    }

    public static async Task<string> AuthenticateWithExistingUserAsync(this HttpClient client, string email,
        string password)
    {
        var loginForm = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = "web-app",
            ["scope"] = "api offline_access"
        };

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(loginForm));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = tokenData!.Token;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return token;
    }

    private class LoginResponse
    {
        [JsonPropertyName("access_token")] public string Token { get; set; } = null!;
        [JsonPropertyName("expires_in")] public int Expiration { get; set; }
    }
}