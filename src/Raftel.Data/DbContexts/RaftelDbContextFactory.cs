using Microsoft.Extensions.DependencyInjection;

namespace Raftel.Data.DbContexts;

public class RaftelDbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory
{
    public TDbContext Create<TDbContext>() where TDbContext : IDbContext
    {
        return serviceProvider.GetService<TDbContext>();
    }
}