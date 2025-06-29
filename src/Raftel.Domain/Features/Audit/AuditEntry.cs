namespace Raftel.Domain.Features.Audit;

/// <summary>
/// Domain representation of an audit entry.
/// </summary>
public sealed record AuditEntry(
    DateTime Timestamp,
    string ChangeType,
    string EntityName,
    string EntityId,
    string? Details,
    List<AuditPropertyChange> PropertyChanges);

/// <summary>
/// Domain representation of an audit property change.
/// </summary>
public sealed record AuditPropertyChange(
    string PropertyName,
    string? OldValue,
    string? NewValue);