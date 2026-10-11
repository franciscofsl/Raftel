namespace Raftel.Application.Abstractions.Concurrency;

/// <summary>
/// Implemented by a query result that exposes the version of the data it was built from. When ETag
/// support is enabled, the API returns that version as the <c>ETag</c> response header.
/// </summary>
public interface IVersionedResult
{
    /// <summary>
    /// Gets the opaque version of the data, or <see langword="null"/> or empty when it has none.
    /// Clients must treat the value as opaque and only send it back unmodified.
    /// </summary>
    string Version { get; }
}
