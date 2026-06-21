namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Converts an arbitrary property value (primitive, value object, or nested value object)
/// into a human-readable string suitable for audit storage. Has no dependency on EF Core.
/// </summary>
public interface IAuditValueSerializer
{
    /// <summary>
    /// Serializes the given value to a string, or <c>null</c> if the value itself is <c>null</c>.
    /// </summary>
    string? Serialize(object? value);
}
