using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raftel.Data;
using Raftel.Data.DataSeed;
using Raftel.Demo.Server;
using Raftel.Shared.Modules;

namespace Raftel.Demo.Migrator;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
                services.AddRaftelApplication<DemoApplication>(configuration);

                var seeders = typeof(Program).Assembly
                    .GetTypes()
                    .Where(type => typeof(ISeeder).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    .ToList();

                seeders.ForEach(seeder => services.AddSingleton(typeof(ISeeder), seeder));
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<IDbContext>();
                await context.Database.MigrateAsync();

                var seedersTasks = services.GetServices<ISeeder>()
                    .Select(_ => _.SeedAsync())
                    .ToList();

                await Task.WhenAll(seedersTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        Console.WriteLine("Migration completed.");
    }
}