using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raftel.Infrastructure.Seeding;

public sealed class SeedHistoryConfiguration : IEntityTypeConfiguration<SeedHistory>
{
    public void Configure(EntityTypeBuilder<SeedHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TenantId);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.AppliedOn)
            .IsRequired();

        builder.HasIndex(x => new { x.Name, x.TenantId })
            .IsUnique()
            .HasFilter(null);
    }
}
