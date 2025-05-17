using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Client;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
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
    public async Task Request_ShouldReturn_FilteredData()
    {
        var filter = new GetPirateByFilterQuery("a", 150000000);

        var response = await _client
            .GetFromJsonAsync<GetPirateByFilterResponse>($"/api/pirates/{QueryFilter.FromObject(filter)}");

        response.Pirates.Count.ShouldBe(4);
    }

    // todo this test must use container databse
    // [Fact] 
    // public async Task Request_ShouldReturn_ExpectedData()
    // {
    //     Guid luffyId = MugiwaraCrew.Luffy().Id;
    //     var response = await _client
    //         .GetFromJsonAsync<GetPirateByIdResponse>($"/api/pirates/{luffyId}");
    //
    //     response.Id.ShouldBe(luffyId);
    //     response.Name.ShouldBe(MugiwaraCrew.Luffy().Name);
    //     response.Bounty.ShouldBe((int)MugiwaraCrew.Luffy().Bounty);
    // }
}