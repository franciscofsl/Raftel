namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// EF Core-agnostic representation of a single entity change, produced by
/// <see cref="IChangeSnapshotExtractor"/> and consumed by <see cref="IAuditLogFactory"/>.
/// </summary>
public sealed record EntityChangeSnapshot(
    string EntityFullName,
    string ChangeType,
    string EntityId,
    IReadOnlyList<PropertyChangeSnapshot> PropertyChanges);
