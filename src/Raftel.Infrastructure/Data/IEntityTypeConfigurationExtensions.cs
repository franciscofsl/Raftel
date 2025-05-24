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
}