namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents the configuration for auditing a specific entity type.
/// </summary>
public class AuditEntityConfiguration
{
    public AuditEntityConfiguration(Type entityType)
    {
        EntityType = entityType;
        EntityName = null; // Will default to EntityType.Name if not set
        AuditChildEntities = true;
        ExcludedProperties = new HashSet<string>();
        IncludedProperties = new HashSet<string>();
    }

    /// <summary>
    /// The entity type being configured for auditing.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The stable name to use for the entity in audit records. 
    /// If not set, defaults to the entity type name.
    /// </summary>
    public string? EntityName { get; private set; }

    /// <summary>
    /// Indicates whether to audit changes to child entities.
    /// </summary>
    public bool AuditChildEntities { get; private set; }

    /// <summary>
    /// Properties to exclude from auditing.
    /// </summary>
    public HashSet<string> ExcludedProperties { get; }

    /// <summary>
    /// Properties to include in auditing (if specified, only these will be audited).
    /// </summary>
    public HashSet<string> IncludedProperties { get; }

    /// <summary>
    /// Sets a stable name for the entity that will be used in audit records.
    /// This ensures audit history remains consistent even if the class name changes during refactoring.
    /// </summary>
    /// <param name="entityName">The stable name to use for this entity type.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public AuditEntityConfiguration WithEntityName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            
        EntityName = entityName;
        return this;
    }

    /// <summary>
    /// Configures whether to audit child entities.
    /// </summary>
    public AuditEntityConfiguration WithChildEntities(bool audit = true)
    {
        AuditChildEntities = audit;
        return this;
    }

    /// <summary>
    /// Excludes specific properties from auditing.
    /// </summary>
    public AuditEntityConfiguration ExcludeProperties(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            ExcludedProperties.Add(propertyName);
        }
        return this;
    }

    /// <summary>
    /// Includes only specific properties in auditing.
    /// </summary>
    public AuditEntityConfiguration IncludeOnlyProperties(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            IncludedProperties.Add(propertyName);
        }
        return this;
    }

    /// <summary>
    /// Determines if a property should be audited based on configuration.
    /// </summary>
    public bool ShouldAuditProperty(string propertyName)
    {
        if (ExcludedProperties.Contains(propertyName))
            return false;

        if (IncludedProperties.Any())
            return IncludedProperties.Contains(propertyName);

        return true;
    }

    /// <summary>
    /// Gets the effective entity name to use in audit records.
    /// Returns the configured EntityName if set, otherwise the entity type name.
    /// </summary>
    public string GetEffectiveEntityName()
    {
        return EntityName ?? EntityType.Name;
    }
}