using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Health;
using Raftel.Infrastructure.Seeding;

namespace Raftel.Infrastructure.Tests.Seeding;

[Collection(SqlServerTestCollection.Name)]
public sealed class DatabaseMigratorTests
{
    private readonly SqlServerTestContainerFixture _fixture;

    public DatabaseMigratorTests(SqlServerTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrateAsync_ShouldApplyPendingMigrations()
    {
        var connectionString = new SqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            InitialCatalog = $"Raftel_Migrator_{Guid.NewGuid():N}"
        }.ConnectionString;
        var services = new ServiceCollection();
        services.AddSampleInfrastructure(connectionString, DatabaseProvider.SqlServer);
        await using var provider = services.BuildServiceProvider();

        try
        {
            using var scope = provider.CreateScope();
            var probe = scope.ServiceProvider.GetRequiredService<IDatabaseProbe>();
            (await probe.GetPendingMigrationsAsync(CancellationToken.None)).ShouldNotBeEmpty();

            await scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync(CancellationToken.None);

            (await probe.GetPendingMigrationsAsync(CancellationToken.None)).ShouldBeEmpty();
        }
        finally
        {
            using var cleanup = provider.CreateScope();
            await cleanup.ServiceProvider.GetRequiredService<TestingRaftelDbContext>().Database.EnsureDeletedAsync();
        }
    }
}
