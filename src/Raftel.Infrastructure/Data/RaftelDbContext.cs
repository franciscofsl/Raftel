using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
            ConfigureBasePropertiesMethodInfo
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, new object[] { modelBuilder, entityType });
        }
    }

    private static readonly MethodInfo ConfigureBasePropertiesMethodInfo
        = typeof(RaftelDbContext<TDbContext>)
            .GetMethod(
                nameof(ConfigureBaseProperties),
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

    protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder,
        IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.IsOwned())
        {
            return;
        }

        ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
    }

    protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder,
        IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.BaseType == null && ShouldFilterEntity<TEntity>(mutableEntityType))
        {
            var filterExpression = CreateFilterExpression<TEntity>(modelBuilder);
            if (filterExpression != null)
            {
                EntityTypeBuilderExtensions.HasQueryFilter(modelBuilder.Entity<TEntity>(), filterExpression);
            }
        }
    }

    protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
    {
        return entityType.FindProperty("IsDeleted") != null;
    }

    protected virtual Expression<Func<TEntity, bool>>? CreateFilterExpression<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? expression = null;
 
        var isDeletedProperty = modelBuilder.Entity<TEntity>().Metadata.FindProperty("IsDeleted");
        if (isDeletedProperty is not null)
        { 
            expression = e => !_dataFilter.IsEnabled<ISoftDeleteFilter>() || 
                              !EF.Property<bool>(e, "IsDeleted");
        }

        return expression;
    }
}

public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// This method is used to add a query filter to this entity which combine with EF Core builtin query filters.
    /// </summary>
    /// <returns></returns>
    public static EntityTypeBuilder<TEntity> HasQueryFilter<TEntity>(this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, bool>> filter)
        where TEntity : class
    {
        var queryFilterAnnotation = builder.Metadata.FindAnnotation(CoreAnnotationNames.QueryFilter);

        if (queryFilterAnnotation != null && queryFilterAnnotation.Value != null &&
            queryFilterAnnotation.Value is Expression<Func<TEntity, bool>> existingFilter)
        {
            filter = QueryFilterExpressionHelper.CombineExpressions(filter, existingFilter);
        }

        return builder.HasQueryFilter(filter);
    }
}

public static class QueryFilterExpressionHelper
{
    public static Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1,
        Expression<Func<T, bool>> expression2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expression1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expression1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expression2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expression2.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

public interface ISoftDeleteFilter
{
}

public interface IDataFilter
{
    bool IsEnabled<TFilter>();
    IDisposable Disable<TFilter>();
}

public class DataFilter : IDataFilter
{
    private static readonly AsyncLocal<HashSet<Type>> _disabledFilters = new();

    public bool IsEnabled<TFilter>()
    {
        var set = _disabledFilters.Value;
        return set == null || !set.Contains(typeof(TFilter));
    }

    public IDisposable Disable<TFilter>()
    {
        var set = _disabledFilters.Value ??= new HashSet<Type>();
        set.Add(typeof(TFilter));
        return new DisposeAction(() => set.Remove(typeof(TFilter)));
    }

    private class DisposeAction : IDisposable
    {
        private readonly Action _onDispose;
        public DisposeAction(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}