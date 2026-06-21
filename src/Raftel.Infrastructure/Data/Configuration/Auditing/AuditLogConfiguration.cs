using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Auditing;
using Raftel.Domain.Auditing.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration.Auditing;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new AuditLogId(value)
            );

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Command)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(256);

        builder.HasMany(x => x.EntityChanges)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.EntityChanges)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Timestamp);
    }
}
