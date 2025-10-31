using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data;

public class DatabaseProviderTests
{
    [Fact]
    public async Task PostgreSql_ShouldBeConfiguredCorrectly()
    {
        var fixture = new PostgreSqlTestContainerFixture();
        await fixture.InitializeAsync();

        try
        {
            var services = new ServiceCollection();
            services.AddSampleInfrastructure(fixture.ConnectionString, DatabaseProvider.PostgreSql);

            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

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
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task SqlServer_ShouldBeConfiguredCorrectly()
    {
        var fixture = new SqlServerTestContainerFixture();
        await fixture.InitializeAsync();

        try
        {
            var services = new ServiceCollection();
            services.AddSampleInfrastructure(fixture.ConnectionString, DatabaseProvider.SqlServer);

            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

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
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DefaultProvider_ShouldBeSqlServer()
    {
        var fixture = new SqlServerTestContainerFixture();
        await fixture.InitializeAsync();

        try
        {
            var services = new ServiceCollection();
            // Not specifying provider - should default to SqlServer
            services.AddSampleInfrastructure(fixture.ConnectionString);

            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
            
            await context.Database.EnsureCreatedAsync();
            
            var isSqlServer = context.Database.IsSqlServer();
            isSqlServer.ShouldBeTrue();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
