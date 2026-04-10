namespace Raftel.Application.Abstractions;

/// <summary>
/// Default implementation of <see cref="IWideEvent"/> that stores properties in a dictionary.
/// Designed to be registered as a scoped service (one instance per request).
/// </summary>
public sealed class WideEvent : IWideEvent
{
    private readonly Dictionary<string, object> _properties = new();

    /// <inheritdoc />
    public void Add(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _properties[key] = value;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetProperties()
    {
        return _properties;
    }
}
