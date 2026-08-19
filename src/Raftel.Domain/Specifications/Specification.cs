using System.Linq.Expressions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Represents a base class for specifications that defines a condition for a given model.
/// </summary>
/// <typeparam name="TModel">The type of the model to which the specification is applied.</typeparam>
public abstract class Specification<TModel>
{
    // Compiled delegate is cached lazily per instance to avoid recompiling the expression tree on every call.
    private readonly Lazy<Func<TModel, bool>> _compiledExpression;

    /// <summary>
    /// Initialises the lazy compiled expression cache for this specification instance.
    /// </summary>
    protected Specification()
    {
        _compiledExpression = new Lazy<Func<TModel, bool>>(() => ToExpression().Compile());
    }

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
        return _compiledExpression.Value(model);
    }

    /// <summary>
    /// Combines the current specification with another specification using a logical AND.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification that represents the logical AND.</returns>
    public Specification<TModel> And(Specification<TModel> other)
    {
        return new AndSpecification<TModel>(this, other);
    }

    /// <summary>
    /// Combines the current specification with another specification using a logical OR.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification that represents the logical OR.</returns>
    public Specification<TModel> Or(Specification<TModel> other)
    {
        return new OrSpecification<TModel>(this, other);
    }
}