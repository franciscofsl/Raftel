using System.Linq.Expressions;
using Raftel.Shared.AdvancedFilters;

namespace Raftel.Shared.Extensions;

public static class ExpressionExtensions
{
    public static Expression Combine(this Expression left, Expression right, Condition condition = Condition.And)
    {
        return condition == Condition.And
            ? Expression.AndAlso(left, right)
            : Expression.OrElse(left, right);
    }
}