using System.Linq.Expressions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Represents a base class for specifications that defines a condition for a given model.
/// </summary>
/// <typeparam name="TModel">The type of the model to which the specification is applied.</typeparam>
public abstract class Specification<TModel>
{
    /// <summary>
    /// Converts the specification to an expression that can be evaluated against a model.
    /// </summary>
    /// <returns>An expression that represents the condition.</returns>
    public abstract Expression<Func<TModel, bool>> ToExpression();

    /// <summary>
    /// Determines whether the model satisfies the specification.
    /// </summary>
    /// <param name="model">The model to evaluate.</param>
    /// <returns>True if the model satisfies the specification, otherwise false.</returns>
    public bool IsSatisfiedBy(TModel model)
    {
        var expression = ToExpression();
        var compiledExpression = expression.Compile();
        return compiledExpression(model);
    }

    /// <summary>
    /// Combines the current specification with another specification using a logical AND.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification that represents the logical AND.</returns>
    public Specification<TModel> And(Specification<TModel> other)
    {
        return new AndIsKingSpecification<TModel>(this, other);
    }

    /// <summary>
    /// Combines the current specification with another specification using a logical OR.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification that represents the logical OR.</returns>
    public Specification<TModel> Or(Specification<TModel> other)
    {
        return new OrIsKingSpecification<TModel>(this, other);
    }
}