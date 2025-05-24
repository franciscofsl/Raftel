using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Client;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

public class MultitenancyTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public MultitenancyTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task ApiShould_CreateTenant()
    {
        await Should.NotThrowAsync(async () =>
        {
            var result = await _client.PostAsJsonAsync("/api/tenants", new
            {
                name = "Tenant Name",
                code = "code",
                description = "Description"
            });
            result.EnsureSuccessStatusCode();
        });
    }

    [Fact]
    public async Task ApiShould_CreateTenant_GetAllTenants_SelectRandomTenant_AndExecuteWithinTenantContext()
    {
        var createTenantResponse = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Tenant Multitenancy Test",
            code = "TEST_MT",
            description = "Tenant Multitenancy Test"
        });
        createTenantResponse.EnsureSuccessStatusCode();

        var getAllTenantsResponse = await _client.GetAsync("/api/tenants");
        getAllTenantsResponse.EnsureSuccessStatusCode();

        var allTenants = await getAllTenantsResponse.Content.ReadFromJsonAsync<List<TenantDto>>();
        allTenants.ShouldNotBeNull();
        allTenants.Count.ShouldBeGreaterThan(0);

        var random = new Random();
        var randomTenant = allTenants[random.Next(allTenants.Count)];

        var requestWithTenant = new HttpRequestMessage(HttpMethod.Get, "/api/tenants/current");
        requestWithTenant.Headers.Add("X-Tenant-Id", randomTenant.Id.ToString());

        var currentTenantResponse = await _client.SendAsync(requestWithTenant);
        currentTenantResponse.EnsureSuccessStatusCode();

        var currentTenantInfo = await currentTenantResponse.Content.ReadFromJsonAsync<CurrentTenantDto>();
        currentTenantInfo.ShouldNotBeNull();
        currentTenantInfo.TenantId.ShouldBe(randomTenant.Id);
        currentTenantInfo.IsMultiTenant.ShouldBeTrue();

        var requestWithoutTenant = new HttpRequestMessage(HttpMethod.Get, "/api/tenants/current");
        var currentTenantWithoutHeaderResponse = await _client.SendAsync(requestWithoutTenant);
        currentTenantWithoutHeaderResponse.EnsureSuccessStatusCode();

        var currentTenantWithoutHeader =
            await currentTenantWithoutHeaderResponse.Content.ReadFromJsonAsync<CurrentTenantDto>();
        currentTenantWithoutHeader.ShouldNotBeNull();
        currentTenantWithoutHeader.TenantId.ShouldBeNull();
        currentTenantWithoutHeader.IsMultiTenant.ShouldBeFalse();
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

    private class CurrentTenantDto
    {
        public Guid? TenantId { get; set; }
        public bool IsMultiTenant { get; set; }
    }
}