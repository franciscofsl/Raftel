namespace Raftel.Data.DbContexts;

public interface IDbContextFactory
{
    TDbContext Create<TDbContext>() where TDbContext : IDbContext;
}