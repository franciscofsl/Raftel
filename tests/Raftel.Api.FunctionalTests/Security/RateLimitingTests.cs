using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Raftel.Api.Server.Middlewares;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Security;

[Collection(ApiTestCollection.Name)]
public sealed class RateLimitingTests : IDisposable
{
    private const int AuthLimit = 5;

    private readonly ApiTestFactory _factory;
    private readonly WebApplicationFactory<DemoApi.Program> _host;
    private readonly HttpClient _client;

    public RateLimitingTests(ApiTestFactory factory)
    {
        _factory = factory;
        _host = SecurityTestHost.With(factory, SecurityTestHost.DefaultRateLimits);
        _client = SecurityTestHost.CreateClient(_host);
    }

    [Fact]
    public async Task SixthFailedLogin_FromTheSameIp_ShouldReturn429WithRetryAfter()
    {
        var ip = "198.51.100.206";
        for (var attempt = 0; attempt < AuthLimit; attempt++)
        {
            (await FailedLoginAsync(ip)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        var response = await FailedLoginAsync(ip);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.ShouldNotBeNull();
        response.Headers.RetryAfter.Delta!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.ShouldBe(StatusCodes.Status429TooManyRequests);
        problem.Extensions["code"]!.ToString().ShouldBe("RateLimit.Exceeded");
    }

    [Fact]
    public async Task RejectedRequest_ShouldEmitItsWideEventAtWarning()
    {
        var ip = "198.51.100.200";
        for (var attempt = 0; attempt < AuthLimit; attempt++)
        {
            await FailedLoginAsync(ip);
        }

        _factory.LogCapture.Clear();
        var response = await FailedLoginAsync(ip);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var wideEvents = _factory.LogCapture.Entries
            .Where(entry => entry.Category == typeof(CorrelationIdMiddleware).FullName)
            .ToList();
        wideEvents.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("rate_limit.rejected"));
    }

    [Fact]
    public async Task AuthLimit_ShouldBePartitionedByIp()
    {
        var exhaustedIp = "198.51.100.201";
        var otherIp = "198.51.100.202";
        for (var attempt = 0; attempt <= AuthLimit; attempt++)
        {
            await FailedLoginAsync(exhaustedIp);
        }

        var response = await FailedLoginAsync(otherIp);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgedForwardedFor_FromAnUntrustedClient_ShouldNotEvadeTheLimit()
    {
        var ip = "198.51.100.203";
        for (var attempt = 0; attempt < AuthLimit; attempt++)
        {
            await FailedLoginAsync(ip);
        }

        var forged = BuildFailedLogin(ip);
        forged.Headers.Add("X-Forwarded-For", "192.0.2.77");
        var response = await _client.SendAsync(forged);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Registration_ShouldShareTheAuthLimit()
    {
        var ip = "198.51.100.204";
        for (var attempt = 0; attempt < AuthLimit; attempt++)
        {
            (await RegisterAsync(ip)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        var response = await RegisterAsync(ip);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task HealthEndpoints_ShouldNotBeRateLimited()
    {
        var ip = "198.51.100.205";
        for (var attempt = 0; attempt < 120; attempt++)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/health/live").From(ip));
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    private Task<HttpResponseMessage> FailedLoginAsync(string ip) => _client.SendAsync(BuildFailedLogin(ip));

    private static HttpRequestMessage BuildFailedLogin(string ip) =>
        new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = $"nobody_{Guid.NewGuid():N}@test.com",
                ["password"] = "WrongPassword1!",
                ["client_id"] = "web-app",
                ["scope"] = "api"
            })
        }.From(ip);

    private Task<HttpResponseMessage> RegisterAsync(string ip) =>
        _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/users/register")
        {
            Content = JsonContent.Create(new { email = $"user_{Guid.NewGuid():N}@test.com", password = "Password123!" })
        }.From(ip));
}
