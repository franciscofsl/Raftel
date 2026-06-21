using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;
using Respawn;

namespace Raftel.Infrastructure.Tests;

public abstract class InfrastructureTestBase : IAsyncLifetime
{
    private static readonly ConcurrentDictionary<DatabaseProvider, Respawner> Respawners = new();

    private readonly IDbContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected InfrastructureTestBase(IDbContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        await context.Database.EnsureCreatedAsync();

        await ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSampleInfrastructure(_fixture.ConnectionString, _fixture.Provider);
    }

    protected TService GetService<TService>() where TService : notnull
    {
        return ServiceProvider.GetRequiredService<TService>();
    }

    protected async Task ExecuteScopedAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        await action(scope.ServiceProvider);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        if (!Respawners.TryGetValue(_fixture.Provider, out var respawner))
        {
            respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = _fixture.RespawnAdapter
            });
            respawner = Respawners.GetOrAdd(_fixture.Provider, respawner);
        }

        await respawner.ResetAsync(connection);
    }

    private DbConnection CreateConnection()
    {
        return _fixture.Provider == DatabaseProvider.PostgreSql
            ? new NpgsqlConnection(_fixture.ConnectionString)
            : new SqlConnection(_fixture.ConnectionString);
    }
}
