using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Web;
using Raftel.Shared.Extensions;

namespace Raftel.Api.Client;

public sealed class QueryFilter
{
    private readonly ImmutableSortedDictionary<string, string> _filters;

    private QueryFilter(ImmutableSortedDictionary<string, string> filters)
    {
        _filters = filters;
    }

    public static QueryFilter Empty() => new(ImmutableSortedDictionary<string, string>.Empty);

    public static QueryFilter FromObject(object? parameters)
    {
        if (parameters == null)
        {
            return Empty();
        }

        var properties = parameters
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return properties
            .Aggregate(Empty(), (filter, property) =>
            {
                var value = property.GetValue(parameters);
                return filter.AddFilter(property.Name, value);
            });
    }

    private QueryFilter AddFilter(string name, object? value)
    {
        if (value == null)
        {
            return this;
        }

        var formattedValue = FormatValue(value);
        var newFilters = _filters.SetItem(name.ToCamelCase(), formattedValue);

        return new QueryFilter(newFilters);
    }

    private static string FormatValue(object value) =>
        value switch
        {
            DateTime dateTime => dateTime.ToString("o"),
            Enum enumValue => enumValue.ToString(),
            IEnumerable enumerable when value is not string => FormatEnumerable(enumerable),
            _ when IsNested(value) => FormatSinglePropertyValue(value),
            _ => value.ToString()!
        };

    private static string FormatEnumerable(IEnumerable enumerable)
    {
        var items = new List<string>();

        foreach (var item in enumerable)
        {
            if (item != null)
            {
                items.Add(FormatValue(item));
            }
        }

        return string.Join(",", items);
    }

    private static bool IsNested(object value)
    {
        var type = value.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Length == 1;
    }

    private static string FormatSinglePropertyValue(object value)
    {
        var property = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Single();

        var innerValue = property.GetValue(value);
        return innerValue != null ? FormatValue(innerValue) : string.Empty;
    }

    public override string ToString()
    {
        if (_filters.IsEmpty)
        {
            return string.Empty;
        }


        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var (name, value) in _filters)
        {
            query[name] = value;
        }

        var queryString = query.ToString();
        return string.IsNullOrEmpty(queryString) ? string.Empty : $"?{queryString}";
    }
}