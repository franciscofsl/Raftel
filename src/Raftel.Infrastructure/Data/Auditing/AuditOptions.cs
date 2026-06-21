namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Options that determine which entity types are tracked by the entity-change audit system.
/// Entities are opt-in: only types explicitly registered via <see cref="Audit{TEntity}"/> are audited.
/// </summary>
public sealed class AuditOptions
{
    private readonly HashSet<Type> _auditedTypes = [];

    /// <summary>
    /// Gets the set of entity types currently registered for auditing.
    /// </summary>
    public IReadOnlySet<Type> AuditedTypes => _auditedTypes;

    /// <summary>
    /// Registers <typeparamref name="TEntity"/> so that its changes are captured by the
    /// entity-change tracking interceptor.
    /// </summary>
    public AuditOptions Audit<TEntity>()
    {
        _auditedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>
    /// Determines whether the given entity type has been registered for auditing.
    /// </summary>
    public bool IsAudited(Type entityType) => _auditedTypes.Contains(entityType);
}
