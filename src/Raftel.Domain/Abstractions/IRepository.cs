using System.Linq.Expressions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Specifications;

namespace Raftel.Domain.Abstractions;

/// <summary>
/// Defines a generic repository interface for managing aggregate root entities.
/// </summary>
/// <typeparam name="TEntity">The type of the aggregate root entity.</typeparam>
/// <typeparam name="TId">The type of the identifier for the aggregate root entity.</typeparam>
public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId> where TId : TypedId<Guid>
{
    /// <summary>
    /// Retrieves an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found, or null otherwise.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all entities.</returns>
    Task<List<TEntity>> ListAllAsync(Expression<Func<TEntity, bool>> filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single page of entities matching an optional filter, ordered by an optional
    /// resolved sort, alongside the total count of matching entities. Never materializes the
    /// full unfiltered/unpaged result set into memory.
    /// </summary>
    /// <param name="page">The page and page size to retrieve.</param>
    /// <param name="filter">An optional filter expression.</param>
    /// <param name="sort">
    /// An optional ordered list of already-resolved sort selectors (see <see cref="SortMap{TEntity}"/>).
    /// A stable tie-break on the entity identifier is always appended, regardless of this value.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the requested page.</returns>
    Task<PagedResult<TEntity>> ListPagedAsync(
        PageRequest page,
        Expression<Func<TEntity, bool>> filter = null,
        IReadOnlyList<SortSelector<TEntity>> sort = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);
}