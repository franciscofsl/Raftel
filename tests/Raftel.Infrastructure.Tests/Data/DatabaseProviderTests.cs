using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data;

[Collection(PostgreSqlTestCollection.Name)]
public class PostgreSqlDatabaseProviderTests
{
    private readonly PostgreSqlTestContainerFixture _fixture;

    public PostgreSqlDatabaseProviderTests(PostgreSqlTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostgreSql_ShouldBeConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        services.AddSampleInfrastructure(_fixture.ConnectionString, DatabaseProvider.PostgreSql);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();

        // Verify database is created
        await context.Database.EnsureCreatedAsync();

        // Verify it's PostgreSQL
        var isNpgsql = context.Database.IsNpgsql();
        isNpgsql.ShouldBeTrue();

        // Verify connection works
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.ShouldBeTrue();
    }
}

[Collection(SqlServerTestCollection.Name)]
public class SqlServerDatabaseProviderTests
{
    private readonly SqlServerTestContainerFixture _fixture;

    public SqlServerDatabaseProviderTests(SqlServerTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SqlServer_ShouldBeConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        services.AddSampleInfrastructure(_fixture.ConnectionString, DatabaseProvider.SqlServer);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();

        // Verify database is created
        await context.Database.EnsureCreatedAsync();

        // Verify it's SQL Server
        var isSqlServer = context.Database.IsSqlServer();
        isSqlServer.ShouldBeTrue();

        // Verify connection works
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.ShouldBeTrue();
    }

    [Fact]
    public async Task DefaultProvider_ShouldBeSqlServer()
    {
        var services = new ServiceCollection();
        // Not specifying provider - should default to SqlServer
        services.AddSampleInfrastructure(_fixture.ConnectionString);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();

        await context.Database.EnsureCreatedAsync();

        var isSqlServer = context.Database.IsSqlServer();
        isSqlServer.ShouldBeTrue();
    }
}
