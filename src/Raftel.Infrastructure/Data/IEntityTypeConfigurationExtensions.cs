using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raftel.Infrastructure.Data;

public static class IEntityTypeConfigurationExtensions
{
    public static void HasSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false)
            .IsRequired();
    }
}