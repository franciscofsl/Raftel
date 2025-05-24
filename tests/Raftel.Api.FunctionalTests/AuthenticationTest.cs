using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Features.Users.GetUserProfile;
using Raftel.Domain.Features.Users;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class AuthenticationTest : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;

    public AuthenticationTest(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    private record RegisterRequest(string Email, string Password);


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

        var response = await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest(email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_NewUser_ShouldSaveIdentityUserId()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var password = "Password123!";

        var response = await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest(email, password));
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var usersRepository = scope.ServiceProvider.GetRequiredService<IUsersRepository>();
        
        var identityUser = await userManager.FindByEmailAsync(email);
        identityUser.ShouldNotBeNull();

        var users = await usersRepository.ListAllAsync();
        var user = users.FirstOrDefault(u => u.Email == email);
        
        user.ShouldNotBeNull();
        user.IdentityUserId.ShouldBe(identityUser.Id);
    }

    [Fact]
    public async Task Login_AfterRegistration_ShouldReturnToken()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var password = "Password123!";
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
            "/api/users/register",
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

        var me = await _client.GetAsync("/api/users/me");
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        var response = await me.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        response.IsAuthenticated.ShouldBeTrue();
        response.UserId.ShouldNotBe(Guid.Empty);
        response.UserName.ShouldBe(email);
    }
}