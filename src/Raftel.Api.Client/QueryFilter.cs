using System.Collections.Immutable;
using System.Reflection;
using System.Web;
using Raftel.Shared.Extensions;

namespace Raftel.Api.Client;

public sealed class QueryFilter
{
    private readonly ImmutableDictionary<string, string> _filters;

    private QueryFilter(ImmutableDictionary<string, string> filters)
    {
        _filters = filters;
    }

    public static QueryFilter Empty => new(ImmutableDictionary<string, string>.Empty);

    public string Build()
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

    public static QueryFilter FromObject(object? parameters)
    {
        var queryFilter = Empty;

        if (parameters == null)
        {
            return queryFilter;
        }

        var properties = parameters.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(_ => _.Name);

        foreach (var property in properties)
        {
            var value = property.GetValue(parameters);
            queryFilter = queryFilter.AddFilter(property.Name, value);
        }

        return queryFilter;
    }

    private static string FormatValue(object value)
    {
        return value is DateTime dateTime
            ? dateTime.ToString("o")
            : value.ToString()!;
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

    public override string ToString()
    {
        return Build();
    }
}