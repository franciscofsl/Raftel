using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.IntegrationTests.Authentication;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Tests;
using Respawn;

namespace Raftel.Application.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private static Respawner? _respawner;

    private readonly SqlServerTestContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected readonly TestCurrentUser CurrentUser = new();

    protected IntegrationTestBase(SqlServerTestContainerFixture fixture)
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
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.AddGlobalMiddleware(typeof(PermissionAuthorizationMiddleware<,>));
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
        });

        services.AddSampleInfrastructure(_fixture.ConnectionString);
        services.AddSingleton<ICurrentUser>(CurrentUser);
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
