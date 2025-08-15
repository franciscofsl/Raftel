using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Shared.Extensions;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Represents a base implementation of a repository using Entity Framework.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TEntity">The type of the aggregate root entity.</typeparam>
/// <typeparam name="TId">The type of the identifier for the aggregate root entity.</typeparam>
/// <param name="dbContext">The database context instance.</param>
public abstract class EfRepository<TDbContext, TEntity, TId>(TDbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : TypedId<Guid>
    where TDbContext : RaftelDbContext<TDbContext>
{
    /// <summary>
    /// Retrieves an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found, or null otherwise.</returns>
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Retrieves all entities asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all entities.</returns>
    public Task<List<TEntity>> ListAllAsync(Expression<Func<TEntity, bool>> filter = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Set<TEntity>()
            .WhereIf(filter is not null, filter)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken token = default)
    {
        if (predicate is null)
        {
            return dbContext.Set<TEntity>().AnyAsync(cancellationToken: token);
        }

        return dbContext.Set<TEntity>().AnyAsync(predicate, token);
    }
}