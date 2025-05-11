using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _container.StartAsync().GetAwaiter().GetResult();
        WaitForDatabaseReady(_container.GetConnectionString()).GetAwaiter().GetResult();

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _container.GetConnectionString()
            };

            configBuilder.AddInMemoryCollection(inMemorySettings);
        });
        
        builder.ConfigureServices(services =>
        { 
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