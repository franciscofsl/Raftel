using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateById;
using Raftel.Domain.Abstractions;
using Raftel.Tests.Common.Domain;
using Shouldly;

namespace Raftel.Api.Integration.Tests;

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

    [Fact]
    public async Task Request_ShouldReturn_ExpectedData()
    {
        Guid luffyId = Mugiwara.Luffy.Id;
        var response = await _client
            .GetFromJsonAsync<GetPirateByIdResponse>($"/api/pirates/{luffyId}");

        response.Id.ShouldBe(Mugiwara.Luffy.Id);
        response.Name.ShouldBe(Mugiwara.Luffy.Name);
        response.Bounty.ShouldBe((int)Mugiwara.Luffy.Bounty);
    }

    [Fact]
    public async Task Swagger_Should_Define_PiratesEndpoint_WithExpectedParameters()
    {
        var swaggerJson = await _client.GetFromJsonAsync<SwaggerDocument>("/swagger/v1/swagger.json");

        swaggerJson.Paths.ShouldContainKey("/api/pirates/{id}");
        swaggerJson.Paths["/api/pirates/{id}"].ShouldContainKey("get");

        var getMethod = swaggerJson.Paths["/api/pirates/{id}"]["get"];

        getMethod.Parameters.ShouldContain(p => p.Name == "id" && p.In == "path");
        getMethod.Parameters.ShouldContain(p => p.Name == "id" && p.In == "path");
        getMethod.Parameters.ShouldContain(p => p.Name == "name" && p.In == "query" && p.Schema.Type == "string");
        getMethod.Parameters.ShouldContain(p => p.Name == "maxBounty" && p.In == "query" && p.Schema.Type == "integer");
    }
}