using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.DemoApi.TestSupport;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

/// <summary>
/// The tenant of a request is bound to the authenticated user: tenant users are pinned to their own tenant,
/// host users need <c>Tenants.Switch</c> to act within a tenant, and only anonymous requests choose it freely.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class TenantResolutionTests
{
    private const string Header = "X-Tenant-Id";
    private const string Password = "Password123!";

    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public TenantResolutionTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task TenantUser_WithoutHeader_ShouldRunWithinItsOwnTenant()
    {
        var tenantA = Guid.NewGuid();
        await LogInAsTenantUserAsync(tenantA);

        (await CurrentTenantAsync(header: null)).ShouldBe(tenantA);
    }

    [Fact]
    public async Task TenantUser_WithItsOwnTenantInTheHeader_ShouldRunWithinItsOwnTenant()
    {
        var tenantA = Guid.NewGuid();
        await LogInAsTenantUserAsync(tenantA);

        (await CurrentTenantAsync(tenantA.ToString())).ShouldBe(tenantA);
    }

    [Fact]
    public async Task TenantUser_WithAnotherTenantInTheHeader_ShouldBeForbidden()
    {
        await LogInAsTenantUserAsync(Guid.NewGuid());

        var response = await SendAsync("/api/test/current-tenant", Guid.NewGuid().ToString());

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task TenantUser_LoggingInWithAnotherTenantInTheHeader_ShouldStillGetATokenPinnedToItsOwnTenant()
    {
        var tenantA = Guid.NewGuid();
        var email = await RegisterAsync(tenantA);

        var token = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = Password,
                ["client_id"] = "web-app",
                ["scope"] = "api offline_access"
            })
        };
        token.Headers.Add(Header, Guid.NewGuid().ToString());
        var tokenResponse = await _client.SendAsync(token);
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", tokenJson.RootElement.GetProperty("access_token").GetString());

        (await CurrentTenantAsync(header: null)).ShouldBe(tenantA);
    }

    [Fact]
    public async Task HostUserWithoutTheSwitchPermission_SendingATenantHeader_ShouldBeForbidden()
    {
        await _client.AuthenticateAsync();

        var response = await SendAsync("/api/test/current-tenant", Guid.NewGuid().ToString());

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "Tenant.Forbidden");
    }

    [Fact]
    public async Task HostUserWithoutTheSwitchPermission_WithoutHeader_ShouldRunAsHost()
    {
        await _client.AuthenticateAsync();

        (await CurrentTenantAsync(header: null)).ShouldBeNull();
    }

    [Fact]
    public async Task Administrator_HoldingTheSwitchPermission_ShouldActWithinTheRequestedTenant()
    {
        var tenantB = Guid.NewGuid();
        await _client.AuthenticateAsAdminAsync(_factory);

        (await CurrentTenantAsync(tenantB.ToString())).ShouldBe(tenantB);
        (await CurrentTenantAsync(header: null)).ShouldBeNull();
    }

    [Fact]
    public async Task Administrator_SwitchingToATenant_ShouldOnlySeeThatTenantsData()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var pirateOfA = $"A {Guid.NewGuid():N}";
        var pirateOfB = $"B {Guid.NewGuid():N}";
        await AddPirateAsync(pirateOfA, tenantA);
        await AddPirateAsync(pirateOfB, tenantB);
        await _client.AuthenticateAsAdminAsync(_factory);

        var response = await SendAsync("/api/pirates", tenantA.ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var names = (await response.Content.ReadFromJsonAsync<GetPirateByFilterResponse>())!.Pirates.Select(p => p.Name).ToList();
        names.ShouldContain(pirateOfA);
        names.ShouldNotContain(pirateOfB);
    }

    [Fact]
    public async Task TenantUser_CannotReadAnotherTenantsData()
    {
        var tenantB = Guid.NewGuid();
        await LogInAsTenantUserAsync(Guid.NewGuid());

        var response = await SendAsync("/api/pirates", tenantB.ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_WithAValidHeader_ShouldRunWithinTheRequestedTenant()
    {
        var tenantId = Guid.NewGuid();

        (await CurrentTenantAsync(tenantId.ToString())).ShouldBe(tenantId);
    }

    [Fact]
    public async Task Anonymous_WithAMalformedHeader_ShouldBeRejectedWith400()
    {
        var response = await SendAsync("/api/test/current-tenant", "not-a-guid");

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Tenant.InvalidHeader");
    }

    private async Task LogInAsTenantUserAsync(Guid tenantId)
    {
        var email = await RegisterAsync(tenantId);
        await _client.AuthenticateWithExistingUserAsync(email, Password);
    }

    private async Task<string> RegisterAsync(Guid tenantId)
    {
        var email = $"tenant_user_{Guid.NewGuid():N}@test.com";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/register")
        {
            Content = JsonContent.Create(new { email, password = Password })
        };
        request.Headers.Add(Header, tenantId.ToString());
        (await _client.SendAsync(request)).EnsureSuccessStatusCode();
        return email;
    }

    private async Task<Guid?> CurrentTenantAsync(string? header)
    {
        var response = await SendAsync("/api/test/current-tenant", header);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CurrentTenantProbe>())!.TenantId;
    }

    private Task<HttpResponseMessage> SendAsync(string url, string? header)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (header is not null)
        {
            request.Headers.Add(Header, header);
        }

        return _client.SendAsync(request);
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().ShouldBe(code);
    }

    private async Task AddPirateAsync(string name, Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        using (services.GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            await services.GetRequiredService<IPirateRepository>().AddAsync(Pirate.Normal(name, 1));
            await services.GetRequiredService<IUnitOfWork>().CommitAsync();
        }
    }
}
