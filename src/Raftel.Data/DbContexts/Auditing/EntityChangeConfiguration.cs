using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Core.Attributes;
using Raftel.Core.Auditing;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityChangeConfiguration : IEntityTypeConfiguration<EntityChange>
{
    public void Configure(EntityTypeBuilder<EntityChange> builder)
    {
        builder.ToTable("EntityChanges");
        builder.HasKey(_ => _.Id);

        builder.Property(_ => _.Kind)
            .HasConversion(_ => _.Id, _ => AuditEventKind.ById(_))
            .HasColumnName("Kind")
            .IsRequired();

        builder
            .HasMany(_ => _.Properties)
            .WithOne();
    }
}