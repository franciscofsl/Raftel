using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Abstractions;

public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId> where TId : TypedId<Guid>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> ListAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}