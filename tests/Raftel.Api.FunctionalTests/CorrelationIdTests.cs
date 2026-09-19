using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Server.Middlewares;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class CorrelationIdTests
{
    private const string HeaderName = "X-Correlation-Id";
    private static readonly string WideEventCategory = typeof(CorrelationIdMiddleware).FullName!;

    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;

    public CorrelationIdTests(ApiTestFactory factory)
    {
        _factory = factory;
        _factory.LogCapture.Clear();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task Request_ShouldEchoBack_KnownCorrelationId()
    {
        const string correlationId = "known-correlation-id-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/error/not-found");
        request.Headers.Add(HeaderName, correlationId);

        var response = await _client.SendAsync(request);

        response.Headers.GetValues(HeaderName).ShouldContain(correlationId);
    }

    [Fact]
    public async Task Request_WithoutCorrelationHeader_ShouldReceiveGeneratedId()
    {
        var response = await _client.GetAsync("/api/test/error/not-found");

        response.Headers.TryGetValues(HeaderName, out var values).ShouldBeTrue();
        values!.Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_WithMaliciousCorrelationHeader_ShouldReceiveSanitizedGeneratedId()
    {
        var maliciousValue = new string('a', 200) + "\r\nX-Injected: true";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/error/not-found");
        request.Headers.TryAddWithoutValidation(HeaderName, maliciousValue);

        var response = await _client.SendAsync(request);

        var returnedValue = response.Headers.GetValues(HeaderName).Single();
        returnedValue.ShouldNotBe(maliciousValue);
        returnedValue.Length.ShouldBeLessThanOrEqualTo(128);
    }

    [Fact]
    public async Task Request_ShouldIncludeCorrelationId_InTheEmittedWideEvent()
    {
        const string correlationId = "known-correlation-id-456";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/error/not-found");
        request.Headers.Add(HeaderName, correlationId);

        await _client.SendAsync(request);

        var wideEvent = _factory.LogCapture.Entries.Single(e => e.Category == WideEventCategory);
        var fields = (IReadOnlyDictionary<string, object>)wideEvent.State;
        fields["request_id"].ShouldBe(correlationId);
    }
}
