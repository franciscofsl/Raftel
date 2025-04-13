using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Data;

public abstract class EfRepository<TDbContext, TEntity, TId>(TDbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : TypedId<Guid>
    where TDbContext : RaftelDbContext<TDbContext>
{
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public  Task<List<TEntity>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return  dbContext.Set<TEntity>().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }
}