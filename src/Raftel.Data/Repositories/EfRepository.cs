using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Raftel.Core.BaseTypes;
using Raftel.Core.Contracts;
using Raftel.Core.Specifications;
using Raftel.Data.DbContexts;
using Raftel.Shared.Exceptions;

namespace Raftel.Data.Repositories;

public class EfRepository<TAggregateRoot, TEntityId>
    : IRepository<TAggregateRoot, TEntityId>
    where TAggregateRoot : AggregateRoot<TEntityId>
    where TEntityId : EntityId
{
    private readonly IDbContextFactory _contextFactory;

    public EfRepository(IDbContextFactory dbContextFactory)
    {
        _contextFactory = dbContextFactory;
    }

    public async Task<TAggregateRoot> GetAsync(TEntityId id)
    {
        var query = GetQueryable();

        var entity = await query.FirstOrDefaultAsync(_ => _.Id == id);

        if (entity is null)
        {
            throw new EntityNotFoundException(id.Value, nameof(TAggregateRoot));
        }

        return entity;
    }

    public Task<List<TAggregateRoot>> GetListAsync(Filter<TAggregateRoot> filter = null)
    {
        var query = GetQueryable();

        if (filter is not null)
        {
            var expression = filter.ToExpression();
            query = query.Where(expression);
        }

        return query.ToListAsync();
    }

    public Task<List<TReturnModel>> GetListAsync<TReturnModel>(Expression<Func<TAggregateRoot, TReturnModel>> map,
        Filter<TAggregateRoot> filter = null)
    {
        var query = GetQueryable();

        if (filter is not null)
        {
            var expressionFilter = filter.ToExpression();
            query = query.Where(expressionFilter);
        }

        return query.Select(map).ToListAsync();
    }

    public Task<bool> AnyAsync(Filter<TAggregateRoot> filter = null)
    {
        var query = GetQueryable();

        return filter is null
            ? query.AnyAsync()
            : query.AnyAsync(filter.ToExpression());
    }

    public async Task<TAggregateRoot> InsertAsync(TAggregateRoot entity, bool save = true)
    {
        var dbContext = CreateDbContext();
        await dbContext.Set<TAggregateRoot>().AddAsync(entity);
        if (save)
        {
            await dbContext.SaveChangesAsync();
        }

        return entity;
    }

    public async Task UpdateAsync(TAggregateRoot entity, bool save = true)
    {
        var dbContext = CreateDbContext();
        dbContext.Set<TAggregateRoot>().Update(entity);
        if (save)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(TAggregateRoot entity, bool save = true)
    {
        var dbContext = CreateDbContext();
        dbContext.Set<TAggregateRoot>().Remove(entity);
        if (save)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    public Task<TAggregateRoot> FirstOrDefaultAsync(Filter<TAggregateRoot> filter = null)
    {
        var query = GetQueryable();

        return filter is null
            ? query.FirstOrDefaultAsync()
            : query.FirstOrDefaultAsync(filter.ToExpression());
    }

    protected IDbContext CreateDbContext()
    {
        return _contextFactory.Create<IDbContext>();
    }

    protected IQueryable<TAggregateRoot> GetQueryable()
    {
        var dbContext = CreateDbContext();
        var query = dbContext.Set<TAggregateRoot>().AsQueryable();

        return ApplyIncludes(query);
    }

    protected virtual IQueryable<TAggregateRoot> ApplyIncludes(IQueryable<TAggregateRoot> query)
    {
        return query;
    }
}