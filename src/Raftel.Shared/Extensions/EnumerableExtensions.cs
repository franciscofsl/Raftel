namespace Raftel.Shared.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition
            ? source.Where(predicate)
            : source;
    }
}