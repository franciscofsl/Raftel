using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Specifications;
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
    where TEntity : AggregateRoot<TId>
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
    /// Retrieves a single page of entities matching an optional filter, ordered by an optional
    /// resolved sort, alongside the total count of matching entities.
    /// </summary>
    /// <param name="page">The page and page size to retrieve.</param>
    /// <param name="filter">An optional filter expression.</param>
    /// <param name="sort">An optional ordered list of already-resolved sort selectors.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the requested page.</returns>
    public async Task<PagedResult<TEntity>> ListPagedAsync(
        PageRequest page,
        Expression<Func<TEntity, bool>> filter = null,
        IReadOnlyList<SortSelector<TEntity>> sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<TEntity>().WhereIf(filter is not null, filter);

        var totalCount = await query.LongCountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<TEntity>.Empty(page);
        }

        var items = await ApplySort(query, sort)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, page.Page, page.PageSize, totalCount);
    }

    /// <summary>
    /// Applies the requested sort selectors in order, always appending a stable tie-break on the
    /// entity identifier so <c>Skip</c>/<c>Take</c> paging never repeats or omits rows across pages.
    /// </summary>
    private static IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, IReadOnlyList<SortSelector<TEntity>> sort)
    {
        IOrderedQueryable<TEntity> ordered = null;

        foreach (var (selector, direction) in sort ?? [])
        {
            ordered = (ordered, direction) switch
            {
                (null, SortDirection.Descending) => query.OrderByDescending(selector),
                (null, _) => query.OrderBy(selector),
                (_, SortDirection.Descending) => ordered.ThenByDescending(selector),
                (_, _) => ordered.ThenBy(selector)
            };
        }

        return ordered is null
            ? query.OrderBy(e => e.Id)
            : ordered.ThenBy(e => e.Id);
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
}