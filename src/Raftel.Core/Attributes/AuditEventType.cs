namespace Raftel.Core.Attributes;

public sealed class AuditEventKind
{
    private AuditEventKind(int kind, string key)
    {
        Id = kind;
        Key = key;
    }

    public int Id { get; }
    public string Key { get; }

    private enum AuditEventTypeKind
    {
        Created,
        Updated,
        Deleted
    }

    public static AuditEventKind Created => new AuditEventKind((int)AuditEventTypeKind.Created, "Created");
    public static AuditEventKind Updated => new AuditEventKind((int)AuditEventTypeKind.Updated, "Updated");
    public static AuditEventKind Deleted => new AuditEventKind((int)AuditEventTypeKind.Deleted, "Deleted");

    public override bool Equals(object obj)
    {
        return obj is AuditEventKind otherKind && Id == otherKind.Id;
    }
}