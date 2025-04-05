using Raftel.Core.BaseTypes;

namespace Raftel.Core.UoW;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<TAggregate, TId> Repository<TAggregate, TId>() where TAggregate : AggregateRoot<TId> where TId : notnull;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}