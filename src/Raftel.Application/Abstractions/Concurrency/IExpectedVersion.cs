namespace Raftel.Application.Abstractions.Concurrency;

/// <summary>
/// Request-scoped holder for the version the caller expects the modified data to still be at,
/// typically taken from an <c>If-Match</c> header. The persistence layer enforces it atomically when
/// the change is saved.
/// </summary>
public interface IExpectedVersion
{
    /// <summary>
    /// Gets the expected version, or <see langword="null"/> when none was supplied or it has been consumed.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Sets the version the caller expects.
    /// </summary>
    /// <param name="version">The opaque version, as previously read from a versioned result.</param>
    void Expect(string version);

    /// <summary>
    /// Returns the expected version and clears it, so it is enforced by exactly one save.
    /// </summary>
    string Consume();
}
