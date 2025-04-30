using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Middlewares;
using Raftel.Tests.Common.Application.Pirates.CreatePirate;
using Raftel.Tests.Common.Infrastructure;
using Raftel.Tests.Common.Infrastructure.Data;

namespace Raftel.Application.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly SqlServerTestContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected IntegrationTestBase()
    {
        _fixture = new SqlServerTestContainerFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        var services = new ServiceCollection();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _fixture.DisposeAsync();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommandHandler).Assembly);
            cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
            cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
        });

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
}