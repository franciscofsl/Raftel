using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raftel.Api.FunctionalTests.TestSupport;
using Raftel.Demo.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Raftel.Api.FunctionalTests;

public sealed class ApiTestFactory : WebApplicationFactory<DemoApi.Program>
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithPassword("yourStrong(!)Password")
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    public TestLogCapture LogCapture { get; } = new();

    public string AdminEmail { get; } = "admin@functional.test";

    public string AdminPassword { get; } = $"Aa1!{Guid.NewGuid():N}";

    /// <summary>
    /// Limits high enough that the shared suite, where every request comes from the same test client,
    /// never hits them. Rate limiting tests start a separate host with the framework defaults.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string?> SuiteRateLimits = new Dictionary<string, string?>
    {
        ["Raftel:Security:RateLimiting:Global:PermitLimit"] = "1000000",
        ["Raftel:Security:RateLimiting:Auth:PermitLimit"] = "1000000",
        ["Raftel:Security:RateLimiting:Write:PermitLimit"] = "1000000"
    };

    private readonly object _containerLock = new();
    private bool _containerStarted;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureContainerStarted();

        builder.ConfigureLogging(logging => logging.AddProvider(new TestLoggerProvider(LogCapture)));

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _container.GetConnectionString(),
                ["Logging:LogLevel:Default"] = "Trace",
                ["Raftel:Seed:AdminEmail"] = AdminEmail,
                ["Raftel:Seed:AdminPassword"] = AdminPassword
            };

            configBuilder.AddInMemoryCollection(inMemorySettings);
            configBuilder.AddInMemoryCollection(SuiteRateLimits);
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestRemoteIpStartupFilter>();

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TestingRaftelDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
 
            services.AddDbContext<TestingRaftelDbContext>(options =>
                options.UseSqlServer(_container.GetConnectionString()));
        });
        builder.UseSetting("https_port", "5128");
    }

    private void EnsureContainerStarted()
    {
        lock (_containerLock)
        {
            if (_containerStarted)
            {
                return;
            }

            _container.StartAsync().GetAwaiter().GetResult();
            WaitForDatabaseReady(_container.GetConnectionString()).GetAwaiter().GetResult();
            _containerStarted = true;
        }
    }

    private async Task WaitForDatabaseReady(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
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
}