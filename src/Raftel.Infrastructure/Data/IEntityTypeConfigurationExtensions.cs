using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Provides extension methods for configuring entity types in Entity Framework Core.
/// </summary>
public static class IEntityTypeConfigurationExtensions
{
    /// <summary>
    /// Configures a shadow property for soft delete functionality on the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being configured.</typeparam>
    /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
    public static void HasSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<bool>(ShadowPropertyNames.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();
    }

    /// <summary>
    /// Configures a shadow property for tenant identification on the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being configured.</typeparam>
    /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
    public static void HasTenantId<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<Guid?>(ShadowPropertyNames.TenantId)
            .HasDefaultValue(null);
    }

    /// <summary>
    /// Configures shadow properties for user auditing (creation and modification tracking) on the specified entity type.
    /// The properties will be automatically populated by an interceptor using <see cref="Application.Abstractions.Authentication.ICurrentUser"/>.
    /// If no authenticated user is available, the UserId properties will remain null.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being configured.</typeparam>
    /// <param name="builder">The <see cref="EntityTypeBuilder{TEntity}"/> used to configure the entity type.</param>
    /// <remarks>
    /// This method configures four shadow properties:
    /// <list type="bullet">
    /// <item><description><see cref="ShadowPropertyNames.CreatorId"/> - The ID of the user who created the entity</description></item>
    /// <item><description><see cref="ShadowPropertyNames.CreationTime"/> - The timestamp when the entity was created</description></item>
    /// <item><description><see cref="ShadowPropertyNames.LastModifierId"/> - The ID of the user who last modified the entity</description></item>
    /// <item><description><see cref="ShadowPropertyNames.LastModificationTime"/> - The timestamp when the entity was last modified</description></item>
    /// </list>
    /// All properties are nullable and do not have referential integrity constraints.
    /// </remarks>
    public static void HasUserAuditing<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<Guid?>(ShadowPropertyNames.CreatorId)
            .HasDefaultValue(null)
            .IsRequired(false);

        builder.Property<DateTime?>(ShadowPropertyNames.CreationTime)
            .HasDefaultValue(null)
            .IsRequired(false);

        builder.Property<Guid?>(ShadowPropertyNames.LastModifierId)
            .HasDefaultValue(null)
            .IsRequired(false);

        builder.Property<DateTime?>(ShadowPropertyNames.LastModificationTime)
            .HasDefaultValue(null)
            .IsRequired(false);
    }
}