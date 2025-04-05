using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Core.UoW;
using Raftel.Data.DbContexts;
using Raftel.Data.Interceptors;
using Raftel.Data.Repositories;
using Raftel.Data.UoW;

namespace Raftel.Data;

public static class ConfigurationExtensions
{
    public static void ConfigureRaftelData<TDbContext>(this IServiceCollection services, IConfiguration configuration)
        where TDbContext : RaftelDbContext<TDbContext>
    {
        services.AddScoped<ConvertDomainEventsToOutboxMessagesInterceptor>();

        services.AddDbContextFactory<TDbContext>((serviceProvider, options) =>
        {
            var interceptor = serviceProvider.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
        services.AddTransient(typeof(IRepository<,>), typeof(EfRepository<,>));
    }
}