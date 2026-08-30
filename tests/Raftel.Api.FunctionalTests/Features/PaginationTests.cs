using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Raftel.Api.FunctionalTests.Extensions;
using Shouldly;

namespace Raftel.Api.FunctionalTests.Features;

[Collection(ApiTestCollection.Name)]
public class PaginationTests
{
    private readonly HttpClient _client;

    public PaginationTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task GetAllTenants_Should_ReturnRequestedPage_Sorted_With_TotalCountHeader()
    {
        await _client.AuthenticateAsync();

        var marker = Guid.NewGuid().ToString("N")[..8];
        foreach (var index in Enumerable.Range(1, 6))
        {
            var response = await _client.PostAsJsonAsync("/api/tenants", new
            {
                name = $"Pagination Tenant {marker} {index:00}",
                code = $"PGT-{marker}-{index:00}",
                description = "Pagination test tenant"
            });
            response.EnsureSuccessStatusCode();
        }

        var firstPageResponse = await _client.GetAsync("/api/tenants?page=1&pageSize=3&sort=-name");
        firstPageResponse.EnsureSuccessStatusCode();

        firstPageResponse.Headers.TryGetValues("X-Total-Count", out var totalCountValues).ShouldBeTrue();
        firstPageResponse.Headers.TryGetValues("X-Total-Pages", out var totalPagesValues).ShouldBeTrue();
        var totalCount = long.Parse(totalCountValues!.Single());

        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<PagedTenantsDto>();
        firstPage.ShouldNotBeNull();
        firstPage.TotalCount.ShouldBe(totalCount);
        firstPage.Items.Count.ShouldBe(3);
        totalPagesValues!.Single().ShouldBe(firstPage.TotalPages.ToString());

        var orderedDescending = firstPage.Items
            .Zip(firstPage.Items.Skip(1), (current, next) => string.CompareOrdinal(current.Name, next.Name) >= 0);
        orderedDescending.ShouldAllBe(inOrder => inOrder);

        var secondPageResponse = await _client.GetAsync("/api/tenants?page=2&pageSize=3&sort=-name");
        secondPageResponse.EnsureSuccessStatusCode();
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<PagedTenantsDto>();
        secondPage.ShouldNotBeNull();

        var firstPageIds = firstPage.Items.Select(t => t.Id).ToHashSet();
        secondPage.Items.ShouldAllBe(tenant => !firstPageIds.Contains(tenant.Id));
    }

    [Fact]
    public async Task GetAllTenants_With_UnknownSortField_Should_ReturnBadRequest()
    {
        await _client.AuthenticateAsync();

        var response = await _client.GetAsync("/api/tenants?sort=secretInternalColumn");

        response.IsSuccessStatusCode.ShouldBeFalse();
        ((int)response.StatusCode).ShouldBe(400);
    }

    [Fact]
    public async Task GetAllTenants_With_PageSizeAboveMaximum_Should_ReturnBadRequest()
    {
        await _client.AuthenticateAsync();

        var response = await _client.GetAsync("/api/tenants?pageSize=999999");

        response.IsSuccessStatusCode.ShouldBeFalse();
        ((int)response.StatusCode).ShouldBe(400);
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    private class PagedTenantsDto
    {
        public List<TenantDto> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
