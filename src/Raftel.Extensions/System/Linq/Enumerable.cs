﻿namespace System.Linq;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> WhereIf<TSource>(this IEnumerable<TSource> source, bool condition,
        Func<TSource, bool> predicate)
    {
        return condition
            ? source.Where(predicate)
            : source;
    }
}