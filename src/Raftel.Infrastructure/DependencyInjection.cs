using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;
using Raftel.Infrastructure.Data.Interceptors;

namespace Raftel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRaftelData<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Default")
        where TDbContext : RaftelDbContext<TDbContext>
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>()));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

        services.AddScoped(typeof(IDataFilter), typeof(DataFilter));
        services.AddScoped<SoftDeleteInterceptor>();

        return services;
    }
}