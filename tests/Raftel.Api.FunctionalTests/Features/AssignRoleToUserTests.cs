using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

[Collection(ApiTestCollection.Name)]
public class AssignRoleToUserTests
{
    private const string Password = "Password123!";

    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public AssignRoleToUserTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task AssignRole_WhenUserDoesNotExist_ShouldReturn404()
    {
        var unknownUserId = Guid.NewGuid();
        var roleId = await FindRoleIdAsync("Admin");
        await _client.AuthenticateAsAdminAsync(_factory);

        var response = await _client.PostAsJsonAsync($"/api/users/{unknownUserId}/roles",
            new { userId = unknownUserId, roleId });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().ShouldBe(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task AssignRole_WhenRoleDoesNotExist_ShouldReturn404()
    {
        var userId = await RegisterUserAsync();
        var unknownRoleId = Guid.NewGuid();
        await _client.AuthenticateAsAdminAsync(_factory);

        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/roles",
            new { userId, roleId = unknownRoleId });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().ShouldBe(RoleErrors.NotFound.Code);
    }

    private async Task<Guid> RegisterUserAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/users/register", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var users = await scope.ServiceProvider.GetRequiredService<IUsersRepository>().ListAllAsync();
        return (Guid)users.Single(u => (string)u.Email == email).Id;
    }

    private async Task<Guid> FindRoleIdAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRolesRepository>().ListAllAsync();
        return (Guid)roles.Single(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Id;
    }
}
