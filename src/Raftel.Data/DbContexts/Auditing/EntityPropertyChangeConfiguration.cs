using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Core.Auditing;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityPropertyChangeConfiguration : IEntityTypeConfiguration<PropertyChange>
{
    public void Configure(EntityTypeBuilder<PropertyChange> builder)
    {
        builder.ToTable("EntityPropertiesChanges");
        builder.HasKey(_ => _.Id);
        builder.Property(_ => _.Name).IsRequired();
        builder.Property(_ => _.TypeName).IsRequired();
    }
}