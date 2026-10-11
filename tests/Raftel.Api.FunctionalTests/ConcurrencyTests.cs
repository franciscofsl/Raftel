using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Tenants;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

[Collection(ApiTestCollection.Name)]
public class ConcurrencyTests
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:5128")
        });
    }

    [Fact]
    public async Task Command_ThatConflictsWithAConcurrentWriter_AnswersConflictProblem()
    {
        var tenantId = await SeedTenantAsync();

        var response = await _client.PostAsJsonAsync("/api/test/concurrency/race", new { tenantId });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Extensions["code"]!.ToString().ShouldBe("Concurrency.Conflict");
    }

    private async Task<Guid> SeedTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var marker = Guid.NewGuid().ToString("N")[..10];
        var tenant = Tenant.Create($"Concurrency {marker}", $"CC-{marker}", "Free").Value;
        context.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }
}
