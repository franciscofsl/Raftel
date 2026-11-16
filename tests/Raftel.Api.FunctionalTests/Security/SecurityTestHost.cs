using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Raftel.Api.FunctionalTests.TestSupport;

namespace Raftel.Api.FunctionalTests.Security;

/// <summary>
/// Starts a separate demo host on the shared database, with security settings layered over the suite's.
/// </summary>
internal static class SecurityTestHost
{
    public static readonly IReadOnlyDictionary<string, string?> DefaultRateLimits = new Dictionary<string, string?>
    {
        ["Raftel:Security:RateLimiting:Global:PermitLimit"] = "100",
        ["Raftel:Security:RateLimiting:Auth:PermitLimit"] = "5",
        ["Raftel:Security:RateLimiting:Write:PermitLimit"] = "30"
    };

    public static WebApplicationFactory<DemoApi.Program> With(
        ApiTestFactory factory,
        IReadOnlyDictionary<string, string?> settings) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings)));

    public static HttpClient CreateClient(WebApplicationFactory<DemoApi.Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost:5128") });

    public static HttpRequestMessage From(this HttpRequestMessage request, string remoteIp)
    {
        request.Headers.Add(TestRemoteIpStartupFilter.HeaderName, remoteIp);
        return request;
    }
}
