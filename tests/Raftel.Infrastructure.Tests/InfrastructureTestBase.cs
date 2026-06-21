using Microsoft.Data.SqlClient;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Respawn;

namespace Raftel.Infrastructure.Tests;

public abstract class InfrastructureTestBase : IAsyncLifetime
{
    private static Respawner? _respawner;

    private readonly SqlServerTestContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected InfrastructureTestBase(SqlServerTestContainerFixture fixture)
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
        services.AddSampleInfrastructure(_fixture.ConnectionString);
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
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });

        await _respawner.ResetAsync(connection);
    }
}
