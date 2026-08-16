using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Queries;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Application.IntegrationTests.Middlewares;

[Collection(IntegrationSqlServerTestCollection.Name)]
public class CancellationTokenPropagationTests : IntegrationTestBase
{
    public CancellationTokenPropagationTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DispatchAsync_Query_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        await ExecuteScopedAsync(async sp =>
        {
            CurrentUser.AddPermission(PiratesPermissions.View);
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var query = new GetPirateByFilterQuery(string.Empty, null);

            await Should.ThrowAsync<OperationCanceledException>(() =>
                queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(query, cts.Token));
        });
    }
}
