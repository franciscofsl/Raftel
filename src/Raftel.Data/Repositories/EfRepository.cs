using Microsoft.EntityFrameworkCore;
using Raftel.Core.BaseTypes;
using Raftel.Core.UoW;

namespace Raftel.Data.Repositories;

public class EfRepository<TAggregate, TId>(DbContext context) : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    protected readonly DbContext Context = context;

    public Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => Context.Set<TAggregate>().FindAsync(new object[] { id! }, cancellationToken).AsTask();

    public Task<List<TAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        =>  Context.Set<TAggregate>().ToListAsync(cancellationToken);

    public Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default)
        => Context.AddAsync(entity, cancellationToken).AsTask();

    public void Update(TAggregate entity) => Context.Update(entity);

    public void Remove(TAggregate entity) => Context.Remove(entity);
}