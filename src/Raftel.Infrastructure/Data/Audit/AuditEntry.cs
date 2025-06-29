using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents a single audit entry that tracks changes to entities.
/// </summary>
public class AuditEntry : Entity<AuditEntryId>
{
    private AuditEntry(
        AuditEntryId id,
        DateTime timestamp,
        string changeType,
        string entityName,
        string entityId,
        string? details = null) : base(id)
    {
        Timestamp = timestamp;
        ChangeType = changeType;
        EntityName = entityName;
        EntityId = entityId;
        Details = details;
        PropertyChanges = new List<AuditPropertyChange>();
    }

    private AuditEntry() : base(AuditEntryId.Empty())
    {
        PropertyChanges = new List<AuditPropertyChange>();
    }

    public DateTime Timestamp { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public List<AuditPropertyChange> PropertyChanges { get; private set; }

    public static AuditEntry Create(
        string changeType,
        string entityName,
        string entityId,
        string? details = null)
    {
        return new AuditEntry(
            AuditEntryId.New(),
            DateTime.UtcNow,
            changeType,
            entityName,
            entityId,
            details);
    }

    public void AddPropertyChange(string propertyName, string? oldValue, string? newValue)
    {
        var propertyChange = AuditPropertyChange.Create(propertyName, oldValue, newValue);
        propertyChange.SetAuditEntryId(Id);
        PropertyChanges.Add(propertyChange);
    }
}