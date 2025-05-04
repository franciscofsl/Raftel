using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raftel.Infrastructure.Data.Filters;

internal static class QueryFilterExtensions
{
    public static EntityTypeBuilder<TEntity> CombineQueryFilter<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, bool>> newFilter)
        where TEntity : class
    {
        var existingFilter = builder.Metadata.GetQueryFilter();
        if (existingFilter is Expression<Func<TEntity, bool>> currentFilter)
        {
            newFilter = newFilter.AndAlso(currentFilter);
        }

        return builder.HasQueryFilter(newFilter);
    }

    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftExpr = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body);
        var rightExpr = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftExpr!, rightExpr!), parameter);
    }

    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}