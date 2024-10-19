using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Raftel.Data.DbContexts.BlobStorage;
using Raftel.Shared.Modules;

namespace Raftel.Infrastructure.Tests;

[ModulesToInclude(typeof(RaftelInfrastructureModule))]
public class RaftelInfrastructureTestApplication : RaftelApplication
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        var mongoConnectionString = configuration.GetConnectionString("MongoDb");
        var mongoClient = new MongoClient(mongoConnectionString);

        services.AddDbContext<BlobDbContext>(opt => opt.UseMongoDB(mongoClient, "Raftel"));
    }
}