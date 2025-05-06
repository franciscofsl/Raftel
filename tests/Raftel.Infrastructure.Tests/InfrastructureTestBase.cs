using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests;

public abstract class InfrastructureTestBase : IAsyncLifetime
{
    private readonly SqlServerTestContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected InfrastructureTestBase()
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