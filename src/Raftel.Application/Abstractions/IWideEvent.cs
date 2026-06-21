namespace Raftel.Application.Abstractions;

/// <summary>
/// Represents a Wide Event that accumulates structured context throughout the lifecycle of a request.
/// A single event is emitted at the end of the request with all collected properties,
/// following the Canonical Log Line pattern.
/// </summary>
public interface IWideEvent
{
    /// <summary>
    /// Adds or overwrites a property in the wide event.
    /// </summary>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value.</param>
    void Add(string key, object value);

    /// <summary>
    /// Gets all properties collected for this event.
    /// </summary>
    /// <returns>A read-only view of all accumulated properties.</returns>
    IReadOnlyDictionary<string, object> GetProperties();
}
