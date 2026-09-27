using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Raftel.Application.Abstractions.Health;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Health;
using Testcontainers.MsSql;

namespace Raftel.Infrastructure.Tests.Health;

public class DatabaseHealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithPassword("yourStrong(!)Password")
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await WaitForDatabaseReadyAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task WaitForDatabaseReadyAsync()
    {
        await using var connection = new SqlConnection(_container.GetConnectionString());
        for (var i = 0; i < 5; i++)
        {
            try
            {
                await connection.OpenAsync();
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new Exception("Cannot connect to database.");
    }

    private static DatabaseHealthCheck CreateCheck(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TestingRaftelDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var dbContext = new TestingRaftelDbContext(options);
        var probe = new DatabaseProbe<TestingRaftelDbContext>(dbContext);

        return new DatabaseHealthCheck(
            probe,
            TimeProvider.System,
            Options.Create(new HealthOptions { DatabaseSlowThreshold = TimeSpan.FromSeconds(30) }));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldTransitionFromHealthyToUnhealthy_WhenContainerStops_AndRecoverAfterRestart()
    {
        var connectionString = _container.GetConnectionString();

        var healthyBefore = await CreateCheck(connectionString).CheckHealthAsync(new HealthCheckContext());
        healthyBefore.Status.ShouldBe(HealthStatus.Healthy);

        await _container.StopAsync();
        var unhealthy = await CreateCheck(connectionString).CheckHealthAsync(new HealthCheckContext());
        unhealthy.Status.ShouldBe(HealthStatus.Unhealthy);

        await _container.StartAsync();
        await WaitForDatabaseReadyAsync();
        var healthyAfter = await CreateCheck(_container.GetConnectionString())
            .CheckHealthAsync(new HealthCheckContext());
        healthyAfter.Status.ShouldBe(HealthStatus.Healthy);
    }
}
