using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Raftel.Api.Server.Middlewares;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class WideEventTests
{
    private static readonly string WideEventCategory = typeof(CorrelationIdMiddleware).FullName!;

    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;

    public WideEventTests(ApiTestFactory factory)
    {
        _factory = factory;
        _factory.LogCapture.Clear();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task SuccessfulCommand_ShouldEmitExactlyOneWideEvent_AtInformationLevel()
    {
        var response = await _client.PostAsJsonAsync("/api/test/resources", new { Name = "widget" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wideEvents = WideEventEntries();
        wideEvents.Count.ShouldBe(1);
        wideEvents[0].Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task BusinessFailure_ShouldEmitExactlyOneWideEvent_AtWarningLevel()
    {
        var response = await _client.GetAsync($"/api/pirates/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var wideEvents = WideEventEntries();
        wideEvents.Count.ShouldBe(1);
        wideEvents[0].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task InPipelineException_ShouldEmitExactlyOneWideEvent()
    {
        await _client.GetAsync("/api/test/throw");

        WideEventEntries().Count.ShouldBe(1);
    }

    [Fact]
    public async Task MalformedJsonBody_ShouldEmitExactlyOneWideEvent()
    {
        using var content = new StringContent("{ not valid json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/test/resources", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WideEventEntries().Count.ShouldBe(1);
    }

    [Fact]
    public async Task NoMatchingRoute_ShouldEmitExactlyOneWideEvent()
    {
        var response = await _client.GetAsync("/api/test/this-route-does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        WideEventEntries().Count.ShouldBe(1);
    }

    private List<TestSupport.CapturedLogEntry> WideEventEntries() =>
        _factory.LogCapture.Entries.Where(e => e.Category == WideEventCategory).ToList();
}
