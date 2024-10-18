using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Raftel.Data;

public interface IDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    public DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}