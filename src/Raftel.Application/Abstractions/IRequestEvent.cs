namespace Raftel.Application.Abstractions;

/// <summary>
/// A request-scoped bag of named fields describing the current HTTP request's lifecycle — the
/// "wide event" / canonical log line emitted once when the request finishes. Framework middleware
/// and application code enrich it throughout the request; exactly one component emits it.
/// </summary>
public interface IRequestEvent
{
    /// <summary>
    /// Gets the fields accumulated so far for the current request.
    /// </summary>
    IReadOnlyDictionary<string, object> Fields { get; }

    /// <summary>
    /// Sets a named field on the current request's event. A field whose name matches the
    /// sensitive-name deny-list (e.g. containing "password", "token", "secret") is silently
    /// dropped instead of stored.
    /// </summary>
    /// <param name="field">The field name (e.g. <c>"user.id"</c>).</param>
    /// <param name="value">The field value.</param>
    void Set(string field, object value);

    /// <summary>
    /// Increments a numeric field on the current request's event by <paramref name="amount"/>,
    /// starting from zero if the field hasn't been set yet. Subject to the same deny-list as
    /// <see cref="Set"/>.
    /// </summary>
    /// <param name="field">The field name (e.g. <c>"db.query_count"</c>).</param>
    /// <param name="amount">The amount to add.</param>
    void Increment(string field, long amount = 1);
}
