using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Raftel.Application;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Abstract base class for the Raftel database context, providing common functionality
/// such as global query filters and unit of work implementation.
/// </summary>
/// <typeparam name="TDbContext">The type of the derived DbContext.</typeparam>
public abstract class RaftelDbContext<TDbContext> : IdentityDbContext, IUnitOfWork
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly IDataFilter _dataFilter;

    protected RaftelDbContext()
    {
    }

    protected RaftelDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    protected RaftelDbContext(DbContextOptions<TDbContext> options, IDataFilter dataFilter) : base(options)
    {
        _dataFilter = dataFilter;
    }

    public DbSet<User> User { get; set; }
    
    protected bool IsSoftDeleteFilterEnabled => _dataFilter?.IsEnabled<ISoftDeleteFilter>() ?? false;


    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaftelDbContext<TDbContext>).Assembly);

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
        if (entityType.BaseType is not null)
        {
            return;
        }

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
            return expression => !IsSoftDeleteFilterEnabled ||
                                 !EF.Property<bool>(expression, ShadowPropertyNames.IsDeleted);
        }

        return null;
    }
}