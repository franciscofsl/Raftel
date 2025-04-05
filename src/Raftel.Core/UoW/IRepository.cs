using Raftel.Core.BaseTypes;

namespace Raftel.Core.UoW;

public interface IRepository<TAggregate, TId> where TAggregate : AggregateRoot<TId> where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<List<TAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default);
    void Update(TAggregate entity);
    void Remove(TAggregate entity);
}