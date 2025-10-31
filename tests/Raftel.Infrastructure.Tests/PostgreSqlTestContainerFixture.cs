using Npgsql;
using Testcontainers.PostgreSql;

namespace Raftel.Infrastructure.Tests;

public class PostgreSqlTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgreSqlTestContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithPassword("postgres")
            .WithImage("postgres:17-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await WaitForDatabaseReady();
    }

    private async Task WaitForDatabaseReady()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        for (var i = 0; i < 3; i++)
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

        throw new Exception("Cant connect to database");
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
