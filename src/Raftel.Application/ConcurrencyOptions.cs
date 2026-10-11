namespace Raftel.Application;

/// <summary>
/// Configures how optimistic concurrency is exposed over HTTP.
/// </summary>
public sealed class ConcurrencyOptions
{
    /// <summary>
    /// Whether the API exchanges versions with clients: an <c>ETag</c> header on versioned query results
    /// and <c>If-Match</c> enforcement on command endpoints. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableETags { get; set; }
}
