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
                name = "Nombre del tenant",
                code = "codigo",
                description = "Descripción del tenant"
            });
            result.EnsureSuccessStatusCode();
        });
    }
}