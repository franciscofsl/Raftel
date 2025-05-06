using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Raftel.Demo.Infrastructure.Data;

public class TestingRaftelDbContextFactory : IDesignTimeDbContextFactory<TestingRaftelDbContext>
{
    public TestingRaftelDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Raftel.Api.FunctionalTests.DemoApi"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("Default");

        var optionsBuilder = new DbContextOptionsBuilder<TestingRaftelDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TestingRaftelDbContext(optionsBuilder.Options);
    }
}
