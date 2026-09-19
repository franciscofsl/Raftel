using System.Linq.Expressions;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Declares the explicit allow-list of fields an entity can be sorted by, resolving a
/// client-supplied field name to the expression to sort with. Fields not declared here
/// are rejected rather than resolved via reflection, so client input never reaches
/// query construction unvalidated.
/// </summary>
/// <typeparam name="TEntity">The entity type the sort fields belong to.</typeparam>
public sealed class SortMap<TEntity>
{
    private readonly Dictionary<string, Expression<Func<TEntity, object>>> _allowed =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Declares a field as sortable, mapping its client-facing name to the expression used to sort by it.
    /// </summary>
    /// <param name="field">The client-facing field name.</param>
    /// <param name="selector">The expression selecting the value to sort by.</param>
    /// <returns>This instance, for fluent chaining.</returns>
    public SortMap<TEntity> Allow(string field, Expression<Func<TEntity, object>> selector)
    {
        _allowed[field] = selector;
        return this;
    }

    /// <summary>
    /// Resolves a client-supplied field name to its sort expression.
    /// </summary>
    /// <param name="field">The client-facing field name.</param>
    /// <returns>A successful result with the sort expression, or a validation failure when the field is not allowed.</returns>
    public Result<Expression<Func<TEntity, object>>> Resolve(string field)
    {
        return _allowed.TryGetValue(field, out var selector)
            ? Result.Success(selector)
            : Result.Failure<Expression<Func<TEntity, object>>>(Error.Validation("Sort.UnknownField",
                $"'{field}' is not a sortable field for {typeof(TEntity).Name}."));
    }

    /// <summary>
    /// Resolves every requested sort field, in order, stopping at the first unknown field.
    /// </summary>
    /// <param name="sortRequests">The client-requested sort fields and directions.</param>
    /// <returns>A successful result with the resolved sort selectors, or the first validation failure encountered.</returns>
    public Result<IReadOnlyList<SortSelector<TEntity>>> ResolveAll(IReadOnlyList<SortRequest> sortRequests)
    {
        if (sortRequests is not { Count: > 0 })
        {
            return Result.Success<IReadOnlyList<SortSelector<TEntity>>>([]);
        }

        var resolved = new List<SortSelector<TEntity>>(sortRequests.Count);

        foreach (var sortRequest in sortRequests)
        {
            var result = Resolve(sortRequest.Field);
            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SortSelector<TEntity>>>(result.Error);
            }

            resolved.Add(new SortSelector<TEntity>(result.Value, sortRequest.Direction));
        }

        return Result.Success<IReadOnlyList<SortSelector<TEntity>>>(resolved);
    }
}
