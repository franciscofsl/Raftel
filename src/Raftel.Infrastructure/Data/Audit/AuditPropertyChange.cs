using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents a property change within an audit entry.
/// </summary>
public class AuditPropertyChange : Entity<AuditPropertyChangeId>
{
    private AuditPropertyChange(
        AuditPropertyChangeId id,
        string propertyName,
        string? oldValue,
        string? newValue) : base(id)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    private AuditPropertyChange() : base(AuditPropertyChangeId.Empty())
    {
    }

    public string PropertyName { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public AuditEntryId AuditEntryId { get; private set; } = AuditEntryId.Empty();

    public static AuditPropertyChange Create(
        string propertyName,
        string? oldValue,
        string? newValue)
    {
        return new AuditPropertyChange(
            AuditPropertyChangeId.New(),
            propertyName,
            oldValue,
            newValue);
    }

    internal void SetAuditEntryId(AuditEntryId auditEntryId)
    {
        AuditEntryId = auditEntryId;
    }
}