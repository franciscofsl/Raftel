using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Raftel.Data.DbContexts;
using Raftel.Data.DbContexts.Auditing;
using Raftel.Data.DbContexts.BlobStorage;
using Raftel.Data.Outbox;

namespace Raftel.Data;

public static class Extensions
{
    public static void AddRaftelDbContext<TDbContext>(this IServiceCollection services, string connectionString = null)
        where TDbContext : DbContext, IDbContext
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        connectionString ??= configuration?.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddScoped<IDbContextFactory, RaftelDbContextFactory>();
            services.AddScoped<AuditChangesInterceptor>();
            services.AddDbContext<IDbContext, TDbContext>(
                (sp, options) => options
                    .UseSqlServer(connectionString)
                    .AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>())
                    .AddInterceptors(sp.GetRequiredService<AuditChangesInterceptor>()));
        }

        var mongoConnectionString = configuration.GetConnectionString("MongoDb");
        if (string.IsNullOrEmpty(mongoConnectionString))
        {
            return;
        }

        var mongoClient = new MongoClient(mongoConnectionString);

        services.AddDbContext<BlobDbContext>(opt => opt.UseMongoDB(mongoClient, "Raftel"));
    }
}