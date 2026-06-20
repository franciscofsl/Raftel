namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// EF Core-agnostic representation of a single property value change, produced by
/// <see cref="IChangeSnapshotExtractor"/> and consumed by <see cref="IAuditLogFactory"/>.
/// </summary>
public sealed record PropertyChangeSnapshot(
    string PropertyName,
    string PropertyType,
    string? OldValue,
    string? NewValue);
