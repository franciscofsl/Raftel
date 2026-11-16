using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Security;

[Collection(ApiTestCollection.Name)]
public sealed class CorsTests : IDisposable
{
    private const string AllowedOrigin = "https://app.example.com";

    private readonly ApiTestFactory _factory;
    private readonly WebApplicationFactory<DemoApi.Program> _host;
    private readonly HttpClient _client;

    public CorsTests(ApiTestFactory factory)
    {
        _factory = factory;
        _host = SecurityTestHost.With(factory, new Dictionary<string, string?>
        {
            ["Raftel:Security:Cors:AllowedOrigins:0"] = AllowedOrigin
        });
        _client = SecurityTestHost.CreateClient(_host);
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_ShouldReturn204WithCorsHeaders()
    {
        var response = await _client.SendAsync(Preflight(AllowedOrigin));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([AllowedOrigin]);
        response.Headers.GetValues("Access-Control-Allow-Methods").Single().ShouldContain("POST");
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_ShouldNotReturnCorsHeaders()
    {
        var response = await _client.SendAsync(Preflight("https://evil.example.com"));

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Response_ToAllowedOrigin_ShouldExposeFrameworkHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _client.SendAsync(request);

        var exposed = response.Headers.GetValues("Access-Control-Expose-Headers").Single();
        exposed.ShouldContain("X-Correlation-Id");
        exposed.ShouldContain("X-Total-Count");
    }

    [Fact]
    public async Task WithoutConfiguredOrigins_CorsShouldBeDisabled()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });

        var response = await client.SendAsync(Preflight(AllowedOrigin));

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/pirates");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");
        return request;
    }
}
