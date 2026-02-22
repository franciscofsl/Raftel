using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class ExceptionHandlingTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500_WithProblemDetailsBody()
    {
        var response = await _client.GetAsync("/api/test/throw");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetails.Title.ShouldBe("Internal Server Error");
    }

    [Fact]
    public async Task UnhandledException_ShouldNotExposeStackTrace()
    {
        var response = await _client.GetAsync("/api/test/throw");

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("at ");
        body.ShouldNotContain("System.InvalidOperationException");
    }
}
