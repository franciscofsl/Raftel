using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.Seeding;

/// <summary>
/// Thrown when seeding fails somewhere a failed <see cref="Result"/> cannot be returned: aborting
/// application startup, or aborting the transaction of an operation that created a tenant.
/// </summary>
public sealed class SeedingFailedException(Error error) : Exception(error.Message)
{
    /// <summary>
    /// Gets the error that caused seeding to fail.
    /// </summary>
    public Error Error { get; } = error;
}
