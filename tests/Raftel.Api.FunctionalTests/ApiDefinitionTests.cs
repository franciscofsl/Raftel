using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.FunctionalTests.ApiDefinition;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class ApiDefinitionTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ApiDefinitionTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task ApiShouldHave_QueriesEndpoints_WithExpectedParameters()
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
    public async Task ApiShouldHave_CommandsEndpoints_WithExpectedParameters()
    {
        var swaggerJson = await _client.GetFromJsonAsync<SwaggerDocument>("/swagger/v1/swagger.json");

        var piratesPath = swaggerJson.Paths["/api/pirates"];

        piratesPath.Count.ShouldBe(2);
        piratesPath.ShouldContainKey("post");
        piratesPath.ShouldContainKey("get");
    }
}