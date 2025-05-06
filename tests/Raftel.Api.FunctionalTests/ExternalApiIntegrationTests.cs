using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Client;
using Raftel.Api.FunctionalTests.ApiDefinition;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class ExternalApiIntegrationTests : IClassFixture<ExternalApiTestFactory>
{
    private readonly HttpClient _client;

    public ExternalApiIntegrationTests(ExternalApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    // todo this test must use container databse
    // [Fact] 
    // public async Task Request_ShouldReturn_ExpectedData()
    // {
    //     Guid luffyId = Mugiwara.Luffy().Id;
    //     var response = await _client
    //         .GetFromJsonAsync<GetPirateByIdResponse>($"/api/pirates/{luffyId}");
    //
    //     response.Id.ShouldBe(luffyId);
    //     response.Name.ShouldBe(Mugiwara.Luffy().Name);
    //     response.Bounty.ShouldBe((int)Mugiwara.Luffy().Bounty);
    // }

    [Fact]
    public async Task Swagger_Should_Define_PiratesEndpoint_WithExpectedParameters()
    {
        var swaggerJson = await _client.GetFromJsonAsync<SwaggerDocument>("/swagger/v1/swagger.json");

        swaggerJson.Paths.ShouldContainKey("/api/pirates/{id}");
        swaggerJson.Paths["/api/pirates/{id}"].ShouldContainKey("get");

        var getMethod = swaggerJson.Paths["/api/pirates/{id}"]["get"];

        getMethod.Parameters.ShouldContain(p => p.Name == "id" && p.In == "path");
        getMethod.Parameters.ShouldContain(p => p.Name == "name" && p.In == "query" && p.Schema.Type == "string");
        getMethod.Parameters.ShouldContain(p => p.Name == "maxBounty" && p.In == "query" && p.Schema.Type == "integer");
    }

    [Fact]
    public async Task Request_ShouldReturn_FilteredData()
    {
        var filter = new GetPirateByFilterQuery("a", 150000000);

        var response = await _client
            .GetFromJsonAsync<GetPirateByFilterResponse>($"/api/pirates/{QueryFilter.FromObject(filter)}");

        response.Pirates.Count.ShouldBe(4);
    }
}