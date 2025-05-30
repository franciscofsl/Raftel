using System.Linq.Expressions;

namespace Raftel.Shared.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition,
        Expression<Func<T, bool>> predicate)
        where T : class
    {
        return condition ? source.Where(predicate) : source;
    }
}