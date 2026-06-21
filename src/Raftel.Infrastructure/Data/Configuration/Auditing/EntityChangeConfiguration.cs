using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Auditing;
using Raftel.Domain.Auditing.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration.Auditing;

public sealed class EntityChangeConfiguration : IEntityTypeConfiguration<EntityChange>
{
    public void Configure(EntityTypeBuilder<EntityChange> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new EntityChangeId(value)
            );

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.EntityFullName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasMany(x => x.AffectedProperties)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.AffectedProperties)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.EntityFullName, x.EntityId });
    }
}
