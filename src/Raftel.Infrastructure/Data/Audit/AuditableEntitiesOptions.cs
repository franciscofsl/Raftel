using System.Collections.Concurrent;

namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Configuration options for specifying which entities should be audited.
/// </summary>
public class AuditableEntitiesOptions
{
    private readonly ConcurrentDictionary<Type, AuditEntityConfiguration> _auditableEntities = new();

    /// <summary>
    /// Registers an entity type for auditing with default configuration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to audit.</typeparam>
    /// <returns>The configuration for further customization.</returns>
    public AuditEntityConfiguration Add<TEntity>() where TEntity : class
    {
        var entityType = typeof(TEntity);
        var configuration = new AuditEntityConfiguration(entityType);
        _auditableEntities.TryAdd(entityType, configuration);
        return configuration;
    }

    /// <summary>
    /// Checks if an entity type is configured for auditing.
    /// </summary>
    /// <param name="entityType">The entity type to check.</param>
    /// <returns>True if the entity should be audited, false otherwise.</returns>
    public bool IsAuditable(Type entityType)
    {
        return _auditableEntities.ContainsKey(entityType);
    }

    /// <summary>
    /// Gets the audit configuration for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The audit configuration if found, null otherwise.</returns>
    public AuditEntityConfiguration? GetConfiguration(Type entityType)
    {
        _auditableEntities.TryGetValue(entityType, out var configuration);
        return configuration;
    }

    /// <summary>
    /// Gets all configured auditable entity types.
    /// </summary>
    /// <returns>A collection of auditable entity types.</returns>
    public IEnumerable<Type> GetAuditableTypes()
    {
        return _auditableEntities.Keys;
    }

    /// <summary>
    /// Gets the effective entity name for a given entity type.
    /// Returns the configured entity name if set, otherwise the type name.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The effective entity name to use in audit records.</returns>
    public string GetEntityName(Type entityType)
    {
        var configuration = GetConfiguration(entityType);
        return configuration?.GetEffectiveEntityName() ?? entityType.Name;
    }
}