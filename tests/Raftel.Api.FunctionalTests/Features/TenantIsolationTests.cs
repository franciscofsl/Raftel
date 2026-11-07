using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

/// <summary>
/// A request resolved without any tenant must only see host data, never the rows of every tenant.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class TenantIsolationTests
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public TenantIsolationTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task RequestWithoutTenant_ShouldNotListPiratesBelongingToATenant()
    {
        var tenantPirate = $"Tenant pirate {Guid.NewGuid():N}";
        var hostPirate = $"Host pirate {Guid.NewGuid():N}";
        await AddPirateAsync(tenantPirate, Guid.NewGuid());
        await AddPirateAsync(hostPirate, tenantId: null);
        await _client.AuthenticateAsAdminAsync(_factory);

        var response = await _client.GetAsync("/api/pirates");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var names = (await response.Content.ReadFromJsonAsync<GetPirateByFilterResponse>())!
            .Pirates.Select(p => p.Name).ToList();
        names.ShouldContain(hostPirate);
        names.ShouldNotContain(tenantPirate);
    }

    [Fact]
    public async Task RequestWithAnInvalidTenantHeader_ShouldNotFallBackToEveryTenantsData()
    {
        var tenantPirate = $"Tenant pirate {Guid.NewGuid():N}";
        await AddPirateAsync(tenantPirate, Guid.NewGuid());
        await _client.AuthenticateAsAdminAsync(_factory);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/pirates");
        request.Headers.Add("X-Tenant-Id", "not-a-guid");
        var response = await _client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var names = (await response.Content.ReadFromJsonAsync<GetPirateByFilterResponse>())!
                .Pirates.Select(p => p.Name);
            names.ShouldNotContain(tenantPirate);
        }
        else
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }

    private async Task AddPirateAsync(string name, Guid? tenantId)
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
