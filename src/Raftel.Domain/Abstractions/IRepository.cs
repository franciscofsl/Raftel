using System.Linq.Expressions;
using Raftel.Domain.BaseTypes;

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
    /// Retrieves a paginated subset of entities asynchronously, optionally filtered.
    /// </summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="filter">An optional filter expression applied before pagination.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing the items for the requested page and the total count of matching entities.</returns>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> ListPagedAsync(int page, int pageSize,
        Expression<Func<TEntity, bool>> filter = null, CancellationToken cancellationToken = default);

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