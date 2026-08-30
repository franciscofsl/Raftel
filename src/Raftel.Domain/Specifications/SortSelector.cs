using System.Linq.Expressions;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Specifications;

/// <summary>
/// Represents an already-resolved, safe-to-use sort expression paired with its direction,
/// produced by <see cref="SortMap{TEntity}"/> from a client-supplied <see cref="SortRequest"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type the sort selector applies to.</typeparam>
/// <param name="Selector">The expression selecting the value to sort by.</param>
/// <param name="Direction">The direction to sort in.</param>
public sealed record SortSelector<TEntity>(Expression<Func<TEntity, object>> Selector, SortDirection Direction);
