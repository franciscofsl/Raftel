using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class ErrorMappingTests
{
    private readonly HttpClient _client;

    public ErrorMappingTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "Test.NotFound")]
    [InlineData("conflict", HttpStatusCode.Conflict, "Test.Conflict")]
    [InlineData("validation", HttpStatusCode.BadRequest, "Test.Validation")]
    [InlineData("forbidden", HttpStatusCode.Forbidden, "Test.Forbidden")]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "Test.Unauthorized")]
    [InlineData("failure", HttpStatusCode.BadRequest, "Test.Failure")]
    public async Task TypedError_ShouldMapToExpectedStatusAndProblemBody(
        string type, HttpStatusCode expectedStatus, string expectedCode)
    {
        var response = await _client.GetAsync($"/api/test/error/{type}");

        response.StatusCode.ShouldBe(expectedStatus);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Extensions["code"]!.ToString().ShouldBe(expectedCode);
    }
}
