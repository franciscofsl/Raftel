namespace Raftel.Infrastructure.Data;

/// <summary>
/// Provides the names of shadow properties used in the application.
/// Shadow properties are properties that are not part of the entity class but are defined in the model.
/// </summary>
public static class ShadowPropertyNames
{
    /// <summary>
    /// The name of the shadow property that indicates whether an entity is soft-deleted.
    /// </summary>
    public const string IsDeleted = nameof(IsDeleted);

    /// <summary>
    /// The name of the shadow property that indicates the tenant identifier for multi-tenant entities.
    /// </summary>
    public const string TenantId = nameof(TenantId);

    /// <summary>
    /// The name of the shadow property that indicates the user who created the entity.
    /// </summary>
    public const string CreatorId = nameof(CreatorId);

    /// <summary>
    /// The name of the shadow property that indicates when the entity was created.
    /// </summary>
    public const string CreationTime = nameof(CreationTime);

    /// <summary>
    /// The name of the shadow property that indicates the user who last modified the entity.
    /// </summary>
    public const string LastModifierId = nameof(LastModifierId);

    /// <summary>
    /// The name of the shadow property that indicates when the entity was last modified.
    /// </summary>
    public const string LastModificationTime = nameof(LastModificationTime);
}