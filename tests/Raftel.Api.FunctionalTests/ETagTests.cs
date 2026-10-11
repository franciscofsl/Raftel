using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Tenants;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class ETagTests
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _clientWithETagsDisabled;

    public ETagTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = CreateClient(factory);
        _clientWithETagsDisabled = CreateClient(factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton(new ConcurrencyOptions { EnableETags = false }))));
    }

    [Fact]
    public async Task GetVersionedResult_ReturnsAQuotedETag()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await _client.GetAsync($"/api/tenants/{tenantId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = response.Headers.ETag;
        etag.ShouldNotBeNull();
        etag.IsWeak.ShouldBeFalse();
        etag.Tag.Length.ShouldBeGreaterThan(2);
    }

    [Fact]
    public async Task ETag_ChangesAfterAWrite_AndIsStableWithoutOne()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var first = (await _client.GetAsync($"/api/tenants/{tenantId}")).Headers.ETag!.Tag;
        var again = (await _client.GetAsync($"/api/tenants/{tenantId}")).Headers.ETag!.Tag;
        var rename = await _client.PutAsJsonAsync("/api/test/concurrency/tenants",
            new { tenantId, name = $"Renamed {Guid.NewGuid():N}"[..20] });
        rename.EnsureSuccessStatusCode();
        var afterWrite = (await _client.GetAsync($"/api/tenants/{tenantId}")).Headers.ETag!.Tag;

        again.ShouldBe(first);
        afterWrite.ShouldNotBe(first);
    }

    [Fact]
    public async Task GetResultWithoutAVersion_HasNoETag()
    {
        await _client.AuthenticateAsync();

        var response = await _client.GetAsync("/api/pirates/paged");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task GetVersionedResult_WithETagsDisabled_HasNoETag()
    {
        await _clientWithETagsDisabled.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await _clientWithETagsDisabled.GetAsync($"/api/tenants/{tenantId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Put_WithMatchingIfMatch_IsApplied()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();
        var etag = await ReadETagAsync(_client, tenantId);

        var response = await PutRenameAsync(_client, tenantId, "Matching", etag);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadNameAsync(_client, tenantId)).ShouldBe("Matching");
    }

    [Fact]
    public async Task Put_WithStaleIfMatch_AnswersPreconditionFailed_AndKeepsTheFirstChange()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();
        var etagReadByA = await ReadETagAsync(_client, tenantId);
        var etagReadByB = await ReadETagAsync(_client, tenantId);

        var first = await PutRenameAsync(_client, tenantId, "Renamed by A", etagReadByA);
        var second = await PutRenameAsync(_client, tenantId, "Renamed by B", etagReadByB);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().ShouldBe("Concurrency.Conflict");
        (await ReadNameAsync(_client, tenantId)).ShouldBe("Renamed by A");
    }

    [Theory]
    [InlineData("\"AAAAAAAAAAA=\"")]
    [InlineData("\"garbage\"")]
    public async Task Put_WithAnIfMatchTheSystemCouldNotHaveIssued_AnswersPreconditionFailed(string ifMatch)
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameAsync(_client, tenantId, "Never applied", ifMatch);

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        (await ReadNameAsync(_client, tenantId)).ShouldNotBe("Never applied");
    }

    [Theory]
    [InlineData("no-quotes")]
    [InlineData("\"one\", \"two\"")]
    [InlineData("W/\"weak\"")]
    public async Task Put_WithAMalformedIfMatch_AnswersBadRequest_WithoutDispatching(string ifMatch)
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameAsync(_client, tenantId, "Never applied", ifMatch);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>()).ShouldNotBeNull();
        (await ReadNameAsync(_client, tenantId)).ShouldNotBe("Never applied");
    }

    [Fact]
    public async Task Put_WithWildcardIfMatch_RunsAsWithoutTheHeader()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameAsync(_client, tenantId, "Wildcard", "*");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadNameAsync(_client, tenantId)).ShouldBe("Wildcard");
    }

    [Fact]
    public async Task Put_WithoutIfMatch_LastWriteWins()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var first = await PutRenameAsync(_client, tenantId, "First", ifMatch: null);
        var second = await PutRenameAsync(_client, tenantId, "Second", ifMatch: null);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadNameAsync(_client, tenantId)).ShouldBe("Second");
    }

    [Fact]
    public async Task Put_WithStaleIfMatch_WhenETagsDisabled_IgnoresTheHeader()
    {
        await _clientWithETagsDisabled.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameAsync(_clientWithETagsDisabled, tenantId, "Ignored header", "\"AAAAAAAAAAA=\"");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RequiredVersion_WithoutIfMatch_AnswersPreconditionRequired_WithoutDispatching()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameRequiringVersionAsync(_client, tenantId, "Never applied", ifMatch: null);

        response.StatusCode.ShouldBe((HttpStatusCode)428);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>()).ShouldNotBeNull();
        (await ReadNameAsync(_client, tenantId)).ShouldNotBe("Never applied");
    }

    [Fact]
    public async Task RequiredVersion_WithWildcard_AnswersPreconditionRequired()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameRequiringVersionAsync(_client, tenantId, "Never applied", "*");

        response.StatusCode.ShouldBe((HttpStatusCode)428);
    }

    [Fact]
    public async Task RequiredVersion_WithMatchingIfMatch_IsApplied()
    {
        await _client.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();
        var etag = await ReadETagAsync(_client, tenantId);

        var response = await PutRenameRequiringVersionAsync(_client, tenantId, "Required ok", etag);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadNameAsync(_client, tenantId)).ShouldBe("Required ok");
    }

    [Fact]
    public async Task RequiredVersion_WhenETagsDisabled_RunsNormally()
    {
        await _clientWithETagsDisabled.AuthenticateAsync();
        var tenantId = await SeedTenantAsync();

        var response = await PutRenameRequiringVersionAsync(_clientWithETagsDisabled, tenantId, "Disabled", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task<string> ReadETagAsync(HttpClient client, Guid tenantId) =>
        (await client.GetAsync($"/api/tenants/{tenantId}")).Headers.ETag!.Tag;

    private static async Task<string> ReadNameAsync(HttpClient client, Guid tenantId) =>
        (await client.GetFromJsonAsync<TenantDto>($"/api/tenants/{tenantId}"))!.Name;

    private static Task<HttpResponseMessage> PutRenameAsync(HttpClient client, Guid tenantId, string name,
        string ifMatch) => PutAsync(client, "/api/test/concurrency/tenants", tenantId, name, ifMatch);

    private static Task<HttpResponseMessage> PutRenameRequiringVersionAsync(HttpClient client, Guid tenantId,
        string name, string ifMatch) =>
        PutAsync(client, "/api/test/concurrency/tenants/versioned", tenantId, name, ifMatch);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string url, Guid tenantId, string name,
        string ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new { tenantId, name })
        };

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return client.SendAsync(request);
    }

    private sealed record TenantDto(string Name);

    private async Task<Guid> SeedTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var marker = Guid.NewGuid().ToString("N")[..10];
        var tenant = Tenant.Create($"ETag {marker}", $"ET-{marker}", "Free").Value;
        context.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    private static HttpClient CreateClient(WebApplicationFactory<DemoApi.Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
}
