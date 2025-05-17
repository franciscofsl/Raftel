using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Client;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class PiratesEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public PiratesEndpointsTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task PostPirate_ShouldCreateAndReturnFilteredPirate()
    {
        const string pirateName = "Created by Functional Test";
        var result = await _client.PostAsJsonAsync("/api/pirates", new
        {
            Name = pirateName,
            Bounty = 150,
            IsKing = false
        });
        result.EnsureSuccessStatusCode();
        
        var filter = new GetPirateByFilterQuery(pirateName, 150000000);

        var response = await _client
            .GetFromJsonAsync<GetPirateByFilterResponse>($"/api/pirates/{QueryFilter.FromObject(filter)}");

        response.Pirates.Count.ShouldBe(1);
        response.Pirates.ShouldContain(_ => _.Name == pirateName);
    }
}