using Microsoft.EntityFrameworkCore;
using Raftel.Core.BaseTypes;
using Raftel.Core.UoW;
using Raftel.Data.DbContexts;
using Raftel.Data.Extensions;
using Raftel.Data.Repositories;

namespace Raftel.Data.UoW;

public class EfUnitOfWork<TDbContext>(IDbContextFactory<TDbContext> contextFactory) : IUnitOfWork
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly TDbContext _context = contextFactory.CreateDbContext();
    private readonly Dictionary<Type, object> _repositories = new();

    public IRepository<TAggregate, TId> Repository<TAggregate, TId>()
        where TAggregate : AggregateRoot<TId> where TId : notnull
    {
        var type = typeof(TAggregate);
        if (_repositories.TryGetValue(type, out var repo))
        {
            return (IRepository<TAggregate, TId>)repo;
        }

        var newRepo = new EfRepository<TAggregate, TId>(_context);
        _repositories[type] = newRepo;
        return newRepo;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _repositories.Clear();
        await _context.DisposeAsync();
    }
}