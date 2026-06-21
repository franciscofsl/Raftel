namespace Raftel.Domain.Auditing;

/// <summary>
/// Represents the kind of change that occurred on an audited entity.
/// </summary>
public static class AuditChangeType
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
}
