using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Raftel.Infrastructure.Tests.Data.Common;

public class SqlServerTestContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public SqlServerTestContainerFixture()
    {
        _container = new MsSqlBuilder()
            .WithPassword("yourStrong(!)Password")
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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
        using var connection = new SqlConnection(ConnectionString);
        for (int i = 0; i < 10; i++)
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