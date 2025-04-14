using System.Linq.Expressions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Represents a logical AND specification that combines two other specifications.
/// </summary>
/// <typeparam name="TModel">The type of the model to which the specification is applied.</typeparam>
public class AndSpecification<TModel>(Specification<TModel> left, Specification<TModel> right) : Specification<TModel>
{
    /// <summary>
    /// Converts the specification to an expression.
    /// </summary>
    /// <returns>A <see cref="Expression{Func{TModel, bool}}"/> that represents the logical AND between the two specifications.</returns>
    public override Expression<Func<TModel, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();
        var parameter = Expression.Parameter(typeof(TModel));

        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }
}