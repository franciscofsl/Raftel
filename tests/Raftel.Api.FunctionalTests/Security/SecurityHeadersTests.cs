using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Security;

[Collection(ApiTestCollection.Name)]
public sealed class SecurityHeadersTests
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/api/pirates")]
    [InlineData("/api/test/this-route-does-not-exist")]
    [InlineData("/api/test/throw")]
    public async Task EveryResponse_ShouldCarryTheSecurityHeaders(string path)
    {
        var response = await _client.GetAsync(path);

        Header(response, "X-Content-Type-Options").ShouldBe("nosniff");
        Header(response, "X-Frame-Options").ShouldBe("DENY");
        Header(response, "Referrer-Policy").ShouldBe("strict-origin-when-cross-origin");
        Header(response, "Content-Security-Policy").ShouldBe("default-src 'none'; frame-ancestors 'none'");
        response.Headers.Contains("Server").ShouldBeFalse();
        response.Headers.Contains("X-Powered-By").ShouldBeFalse();
    }

    [Fact]
    public async Task Hsts_ShouldNotBeSentInDevelopment()
    {
        var response = await _client.GetAsync("/health/live");

        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    [Fact]
    public async Task DocumentationPath_ShouldGetTheRelaxedContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Header(response, "Content-Security-Policy").ShouldContain("script-src 'self'");
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.Single()
            : response.Content.Headers.TryGetValues(name, out var contentValues) ? contentValues.Single() : null;
}
