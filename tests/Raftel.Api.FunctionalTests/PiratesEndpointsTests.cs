using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.Client;
using Raftel.Api.FunctionalTests.Extensions;
using Raftel.Application.Queries;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Application.Pirates.GetPiratesPaged;
using Raftel.Demo.Domain.Pirates;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class PiratesEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public PiratesEndpointsTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task PostPirate_ShouldCreateAndReturnFilteredPirate()
    {
        await _client.AuthenticateAsync();
        
        const string pirateName = "Created by Functional Test";
        var result = await _client.PostAsJsonAsync("/api/pirates", new
        {
            Name = pirateName,
            Bounty = 150,
            IsKing = false
        });
        result.EnsureSuccessStatusCode();
        
        var filter = new GetPirateByFilterQuery(pirateName, 150000000);

        var response = await _client
            .GetFromJsonAsync<GetPirateByFilterResponse>($"/api/pirates/{QueryFilter.FromObject(filter)}");

        response.Pirates.Count.ShouldBe(1);
        response.Pirates.ShouldContain(_ => _.Name == pirateName);
    }

    [Fact]
    public async Task PostPirate_WithMalformedJson_ShouldReturnBadRequest()
    {
        await _client.AuthenticateAsync();

        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/pirates", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPiratesPaged_ShouldReturnPagedResult_WithValidParameters()
    {
        await _client.AuthenticateAsync();

        var pirateName = $"Paged Pirate {Guid.NewGuid():N}";
        var createResult = await _client.PostAsJsonAsync("/api/pirates", new
        {
            Name = pirateName,
            Bounty = 500,
            IsKing = false
        });
        createResult.EnsureSuccessStatusCode();

        var query = new GetPiratesPagedQuery(1, 10, pirateName);
        var response = await _client
            .GetFromJsonAsync<PagedResponse>($"/api/pirates/paged{QueryFilter.FromObject(query)}");

        response.ShouldNotBeNull();
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(10);
        response.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        response.Items.ShouldContain(p => p.Name == pirateName);
    }

    [Fact]
    public async Task GetPiratesPaged_WithInvalidPage_ShouldReturnBadRequest()
    {
        await _client.AuthenticateAsync();

        var query = new GetPiratesPagedQuery(0, 10, null);
        var response = await _client
            .GetAsync($"/api/pirates/paged{QueryFilter.FromObject(query)}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPiratesPaged_WithPageSizeExceedingMaximum_ShouldReturnBadRequest()
    {
        await _client.AuthenticateAsync();

        var query = new GetPiratesPagedQuery(1, 101, null);
        var response = await _client
            .GetAsync($"/api/pirates/paged{QueryFilter.FromObject(query)}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPiratesPaged_WithPageBeyondAvailableData_ShouldReturnEmptyItems()
    {
        await _client.AuthenticateAsync();

        var query = new GetPiratesPagedQuery(999, 10, $"NoSuchPirate_{Guid.NewGuid():N}");
        var response = await _client
            .GetFromJsonAsync<PagedResponse>($"/api/pirates/paged{QueryFilter.FromObject(query)}");

        response.ShouldNotBeNull();
        response.Items.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
    }

    private sealed class PagedResponse
    {
        public List<PirateItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class PirateItem
    {
        public string Name { get; set; }
        public uint Bounty { get; set; }
    }
}