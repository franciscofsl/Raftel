using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Raftel.Api.Server.Middlewares;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class ExceptionHandlingTests
{
    private static readonly string WideEventCategory = typeof(CorrelationIdMiddleware).FullName!;

    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;

    public ExceptionHandlingTests(ApiTestFactory factory)
    {
        _factory = factory;
        _factory.LogCapture.Clear();
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

    [Fact]
    public async Task UnhandledException_ShouldEmitExactlyOneWideEvent_AtErrorLevel()
    {
        await _client.GetAsync("/api/test/throw");

        var wideEvents = WideEventEntries();
        wideEvents.Count.ShouldBe(1);
        wideEvents[0].Level.ShouldBe(LogLevel.Error);
        wideEvents[0].Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidationException_ShouldEmitWideEvent_AtDebugLevel()
    {
        await _client.GetAsync("/api/test/throw/validation");

        var wideEvents = WideEventEntries();
        wideEvents.Count.ShouldBe(1);
        wideEvents[0].Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public async Task UnauthorizedException_ShouldEmitWideEvent_AtWarningLevel_WithRequiredPermission()
    {
        await _client.GetAsync("/api/test/throw/unauthorized");

        var wideEvents = WideEventEntries();
        wideEvents.Count.ShouldBe(1);
        wideEvents[0].Level.ShouldBe(LogLevel.Warning);
        wideEvents[0].Exception.Message.ShouldContain("test.permission");
    }

    private List<TestSupport.CapturedLogEntry> WideEventEntries() =>
        _factory.LogCapture.Entries.Where(e => e.Category == WideEventCategory).ToList();
}
