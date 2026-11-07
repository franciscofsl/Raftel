using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application.Commands;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Tenants.CreateTenant;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Users;
using Raftel.Domain.ValueObjects;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

/// <summary>
/// Every endpoint whose request carries <c>[RequiresPermission]</c> must reject anonymous callers (401) and
/// authenticated callers missing the permission (403) without running the handler, and must still serve a
/// caller holding the permission.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class PermissionEnforcementTests
{
    private const string Password = "Password123!";

    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public PermissionEnforcementTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    public static TheoryData<string, string, string?> ProtectedEndpoints()
    {
        var id = Guid.NewGuid();
        return new TheoryData<string, string, string?>
        {
            { "GET", "/api/tenants", null },
            { "POST", "/api/tenants", """{"name":"Denied","code":"DENIED","description":"Denied"}""" },
            { "GET", $"/api/tenants/{id}", null },
            { "GET", "/api/tenants/current", null },
            { "GET", "/api/users/me", null },
            { "POST", "/api/users", """{"email":"denied@test.com","name":"N","surname":"S","password":"Password123!"}""" },
            { "PUT", $"/api/users/{id}", $$"""{"userId":"{{id}}","name":"N","surname":"S","email":"denied@test.com"}""" },
            { "DELETE", $"/api/users/{id}", $$"""{"userId":"{{id}}"}""" },
            { "POST", $"/api/users/{id}/roles", $$"""{"userId":"{{id}}","roleId":"{{Guid.NewGuid()}}"}""" },
            { "POST", "/api/pirates", """{"name":"Denied","bounty":1}""" },
            { "GET", "/api/pirates", null },
            { "GET", "/api/pirates/paged", null }
        };
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WhenCallerIsAnonymous_ShouldReturn401(string method, string url, string? body)
    {
        var response = await _client.SendAsync(BuildRequest(method, url, body));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WhenCallerLacksThePermission_ShouldReturn403(string method, string url, string? body)
    {
        await _client.AuthenticateAsync();

        var response = await _client.SendAsync(BuildRequest(method, url, body));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenTokenIsInvalid_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTenant_WhenCallerLacksThePermission_ShouldNotCreateTheTenant()
    {
        var code = UniqueCode();
        await _client.AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/tenants", new { name = "Denied", code, description = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await TenantCodeIsFreeAsync(code)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenCallerLacksThePermission_ShouldNotDeleteTheTargetUser()
    {
        var victim = await RegisterUserAsync();
        await _client.AuthenticateAsync();

        var response = await _client.SendAsync(BuildRequest("DELETE", $"/api/users/{victim.Id}", $$"""{"userId":"{{victim.Id}}"}"""));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await FindUserAsync(victim.Email)).ShouldNotBeNull();
    }

    [Fact]
    public async Task EditUser_WhenCallerLacksThePermission_ShouldNotChangeTheTargetUser()
    {
        var victim = await RegisterUserAsync();
        var attackerEmail = $"attacker_{Guid.NewGuid():N}@test.com";
        await _client.AuthenticateAsync();

        var response = await _client.PutAsJsonAsync($"/api/users/{victim.Id}",
            new { userId = victim.Id, name = "Hacked", surname = "Hacked", email = attackerEmail });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var stored = await FindUserAsync(victim.Email);
        stored.ShouldNotBeNull();
        stored.Name.ShouldNotBe("Hacked");
        (await FindUserAsync(attackerEmail)).ShouldBeNull();
    }

    [Fact]
    public async Task AssignRole_WhenCallerTriesToGrantThemselvesTheAdministratorRole_ShouldBeForbiddenAndGrantNothing()
    {
        var attacker = await RegisterUserAsync();
        var adminRoleId = await FindRoleIdAsync("Admin");
        await _client.AuthenticateWithExistingUserAsync(attacker.Email, Password);

        var response = await _client.PostAsJsonAsync($"/api/users/{attacker.Id}/roles",
            new { userId = attacker.Id, roleId = adminRoleId });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await _client.AuthenticateWithExistingUserAsync(attacker.Email, Password);
        (await HasPermissionAsync(TenantsPermissions.Management)).ShouldBeFalse();
    }

    [Fact]
    public async Task ProtectedEndpoints_WhenCallerHoldsThePermissions_ShouldServeTheRequest()
    {
        var code = UniqueCode();
        await _client.AuthenticateAsAdminAsync(_factory);

        var create = await _client.PostAsJsonAsync("/api/tenants", new { name = "Allowed", code, description = "x" });
        var list = await _client.GetAsync("/api/tenants");
        var me = await _client.GetAsync("/api/users/me");
        var pirates = await _client.GetAsync("/api/pirates/paged");

        create.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        pirates.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await TenantCodeIsFreeAsync(code)).ShouldBeFalse();
    }

    [Fact]
    public async Task AnonymousEndpoints_ShouldStayReachableWithoutAToken()
    {
        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { email = $"user_{Guid.NewGuid():N}@test.com", password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ProtectedCommand_DispatchedOutsideAnHttpRequest_ShouldBeRejectedAsUnauthorizedAndNotRun()
    {
        var code = UniqueCode();
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.DispatchAsync(new CreateTenantCommand("Background", code, "x"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        (await TenantCodeIsFreeAsync(code)).ShouldBeTrue();
    }

    private static HttpRequestMessage BuildRequest(string method, string url, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string UniqueCode() => $"C{Guid.NewGuid():N}"[..20];

    private async Task<(Guid Id, string Email)> RegisterUserAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/users/register", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        var user = await FindUserAsync(email);
        return ((Guid)user!.Id, email);
    }

    private async Task<User?> FindUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = await scope.ServiceProvider.GetRequiredService<IUsersRepository>().ListAllAsync();
        return users.FirstOrDefault(u => (string)u.Email == email);
    }

    private async Task<Guid> FindRoleIdAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRolesRepository>().ListAllAsync();
        return (Guid)roles.Single(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Id;
    }

    private async Task<bool> TenantCodeIsFreeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ITenantsRepository>()
            .CodeIsUniqueAsync(Code.Create(code).Value, CancellationToken.None);
    }

    private async Task<bool> HasPermissionAsync(string permission)
    {
        var response = await _client.GetAsync($"/api/test/permissions/{permission}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
