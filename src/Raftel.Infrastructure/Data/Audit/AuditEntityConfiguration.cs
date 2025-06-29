namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Represents the configuration for auditing a specific entity type.
/// </summary>
public class AuditEntityConfiguration
{
    public AuditEntityConfiguration(Type entityType)
    {
        EntityType = entityType;
        AuditCreation = true;
        AuditUpdate = true;
        AuditDeletion = true;
        AuditChildEntities = true;
        ExcludedProperties = new HashSet<string>();
        IncludedProperties = new HashSet<string>();
    }

    /// <summary>
    /// The entity type being configured for auditing.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Indicates whether to audit entity creation.
    /// </summary>
    public bool AuditCreation { get; private set; }

    /// <summary>
    /// Indicates whether to audit entity updates.
    /// </summary>
    public bool AuditUpdate { get; private set; }

    /// <summary>
    /// Indicates whether to audit entity deletion.
    /// </summary>
    public bool AuditDeletion { get; private set; }

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
    /// Configures whether to audit entity creation.
    /// </summary>
    public AuditEntityConfiguration WithCreation(bool audit = true)
    {
        AuditCreation = audit;
        return this;
    }

    /// <summary>
    /// Configures whether to audit entity updates.
    /// </summary>
    public AuditEntityConfiguration WithUpdate(bool audit = true)
    {
        AuditUpdate = audit;
        return this;
    }

    /// <summary>
    /// Configures whether to audit entity deletion.
    /// </summary>
    public AuditEntityConfiguration WithDeletion(bool audit = true)
    {
        AuditDeletion = audit;
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
}