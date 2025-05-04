using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Raftel.Application;

namespace Raftel.Infrastructure.Data;

public abstract class RaftelDbContext<TDbContext> : DbContext, IUnitOfWork
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly IDataFilter _dataFilter;

    protected RaftelDbContext()
    {
    }

    protected RaftelDbContext(DbContextOptions<TDbContext> options, IDataFilter dataFilter) : base(options)
    {
        _dataFilter = dataFilter;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var method = typeof(RaftelDbContext<TDbContext>)
                .GetMethod(nameof(ConfigureEntityFilters), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, new object[] { modelBuilder, entityType });
        }
    }

    private void ConfigureEntityFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType entityType)
        where TEntity : class
    {
        if (entityType.BaseType != null) return;

        var filter = BuildGlobalFilterExpression<TEntity>(entityType);
        if (filter is not null)
        {
            modelBuilder.Entity<TEntity>().CombineQueryFilter(filter);
        }
    }

    private Expression<Func<TEntity, bool>> BuildGlobalFilterExpression<TEntity>(IMutableEntityType entityType)
        where TEntity : class
    {
        if (entityType.FindProperty(ShadowPropertyNames.IsDeleted)?.ClrType == typeof(bool))
        {
            return expression => !_dataFilter.IsEnabled<ISoftDeleteFilter>() ||
                                 !EF.Property<bool>(expression, ShadowPropertyNames.IsDeleted);
        }

        return null;
    }
}