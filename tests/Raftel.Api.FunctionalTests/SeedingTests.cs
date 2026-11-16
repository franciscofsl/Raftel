using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application.Authorization;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Users;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;
using Raftel.Infrastructure.Seeding;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class SeedingTests
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public SeedingTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task StartedAgainstAnEmptyDatabase_ShouldPersistTheCatalogOfEveryPermissionSet()
    {
        using var scope = _factory.Services.CreateScope();
        var names = await scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>()
            .Set<PermissionDefinition>().Select(d => d.Name).ToListAsync();

        names.ShouldContain(PiratesPermissions.Management);
        names.ShouldContain(PiratesPermissions.View);
        names.ShouldContain(UsersPermissions.Create);
        names.ShouldContain(TenantsPermissions.Management);
        names.ShouldContain(TenantsPermissions.View);
    }

    [Fact]
    public async Task SeededAdministrator_ShouldLogInAndHoldEveryDeclaredPermission()
    {
        await _client.AuthenticateWithExistingUserAsync(_factory.AdminEmail, _factory.AdminPassword);

        var declared = _factory.Services.GetRequiredService<IPermissionSetProvider>().PermissionNames;
        declared.ShouldNotBeEmpty();
        foreach (var permission in declared)
        {
            (await HasPermissionAsync(permission)).ShouldBeTrue(permission);
        }
    }

    [Fact]
    public async Task OrdinaryUser_ShouldHoldNoSeededPermission()
    {
        await _client.AuthenticateAsync();

        (await HasPermissionAsync(TenantsPermissions.Management)).ShouldBeFalse();
    }

    private async Task<bool> HasPermissionAsync(string permission)
    {
        var response = await _client.GetAsync($"/api/test/permissions/{permission}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_ShouldReturn503WhileSeedingRunsAnd200AfterwardsAndDegradedWhenItFinishedWithFailures()
    {
        var state = _factory.Services.GetRequiredService<SeedingState>();
        try
        {
            state.MarkRunning();
            (await _client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

            state.MarkCompleted();
            (await _client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);

            state.MarkCompletedWithFailures();
            var degraded = await _client.GetAsync("/health/ready");
            degraded.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await degraded.Content.ReadAsStringAsync()).ShouldContain("Degraded", Case.Insensitive);
        }
        finally
        {
            state.MarkCompleted();
        }
    }

    [Fact]
    public async Task Ready_ShouldReturn200WhenSeedingWasNeverRequested()
    {
        var state = new SeedingState();
        state.Status.ShouldBe(SeedingStatus.NotRequested);

        (await new Raftel.Infrastructure.Health.SeedingHealthCheck(state).CheckHealthAsync(new()))
            .Status.ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }
}
