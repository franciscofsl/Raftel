using System.Linq.Expressions;
using Raftel.Core.BaseTypes;
using Raftel.Core.Specifications;

namespace Raftel.Core.Contracts;

public interface IRepository<TAggregateRoot, TEntityId>
    where TAggregateRoot : AggregateRoot<TEntityId>
    where TEntityId : EntityId
{
    public Task<TAggregateRoot> GetAsync(TEntityId id);

    public Task<TAggregateRoot> InsertAsync(TAggregateRoot entity, bool save = true);

    public Task UpdateAsync(TAggregateRoot entity, bool save = true);

    public Task DeleteAsync(TAggregateRoot entity, bool save = true);

    Task<List<TAggregateRoot>> GetListAsync(Filter<TAggregateRoot> filter = null);

    Task<List<TReturnModel>> GetListAsync<TReturnModel>(Expression<Func<TAggregateRoot, TReturnModel>> map,
        Filter<TAggregateRoot> filter = null);

    Task<bool> AnyAsync(Filter<TAggregateRoot> filter = null);
}