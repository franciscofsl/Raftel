using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Shared.Modules;

namespace Raftel.Testing;

public abstract class TestBase<TRaftelApplication> where TRaftelApplication : RaftelApplication
{
    protected TestBase()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        Application = services.AddRaftelApplication<TRaftelApplication>(configuration);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected TRaftelApplication Application { get; }

    protected IServiceProvider ServiceProvider { get; }

    protected T GetService<T>()
    {
        return ServiceProvider.GetService<T>();
    }

    protected T GetRequiredService<T>()
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}