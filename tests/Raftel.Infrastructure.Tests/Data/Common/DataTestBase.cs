using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Tests.Common.Application;

namespace Raftel.Infrastructure.Tests.Data.Common;

public abstract class DataTestBase : IAsyncLifetime
{
    private readonly SqlServerTestContainerFixture _fixture;

    protected IServiceProvider ServiceProvider { get; private set; } = default!;

    protected DataTestBase()
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = _fixture.ConnectionString
            })
            .Build();
        services.AddRaftelData<TestingRaftelDbContext>(configuration, "TestConnection");
        services.AddRaftelApplication();

        services.AddScoped(typeof(IPirateRepository), typeof(PirateRepository));
        services.AddScoped<IRequestHandler<CreatePirateCommand, Result>, CreatePirateCommandHandler>();
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