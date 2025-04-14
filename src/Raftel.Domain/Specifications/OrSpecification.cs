using System.Linq.Expressions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Represents a logical OR specification that combines two other specifications.
/// </summary>
/// <typeparam name="TModel">The type of the model to which the specification is applied.</typeparam>
public class OrSpecification<TModel> : Specification<TModel>
{
    private readonly Specification<TModel> _left;
    private readonly Specification<TModel> _right;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{TModel}"/> class.
    /// </summary>
    /// <param name="left">The left specification to combine.</param>
    /// <param name="right">The right specification to combine.</param>
    public OrSpecification(Specification<TModel> left, Specification<TModel> right)
    {
        _left = left;
        _right = right;
    }

    /// <summary>
    /// Converts the specification to an expression.
    /// </summary>
    /// <returns>A <see cref="Expression{Func{TModel, bool}}"/> that represents the logical OR between the two specifications.</returns>
    public override Expression<Func<TModel, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(TModel));

        var body = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }
}