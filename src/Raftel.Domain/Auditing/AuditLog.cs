using Raftel.Domain.Auditing.ValueObjects;
using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Auditing;

/// <summary>
/// Aggregate root that records the entity changes produced while handling a single
/// Raftel command or query.
/// </summary>
public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    private readonly List<EntityChange> _entityChanges = [];

    // Future iteration: a child collection of exceptions raised while handling the command/query.

    private AuditLog(
        AuditLogId id,
        DateTime timestamp,
        string command,
        Guid? userId,
        string? userName,
        string? details) : base(id)
    {
        Timestamp = timestamp;
        Command = command;
        UserId = userId;
        UserName = userName;
        Details = details;
    }

    private AuditLog()
    {
    }

    public DateTime Timestamp { get; private set; }
    public string Command { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? Details { get; private set; }

    public IReadOnlyCollection<EntityChange> EntityChanges => _entityChanges.AsReadOnly();

    public static AuditLog For(
        string command,
        DateTime timestamp,
        Guid? userId = null,
        string? userName = null,
        string? details = null) =>
        new(AuditLogId.New(), timestamp, command, userId, userName, details);

    public void RegisterChange(EntityChange entityChange)
    {
        _entityChanges.Add(entityChange);
    }

    public bool HasChanges => _entityChanges.Count > 0;
}
