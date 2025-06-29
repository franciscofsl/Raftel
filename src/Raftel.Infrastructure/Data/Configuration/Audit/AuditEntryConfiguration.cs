using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Infrastructure.Data.Audit;

namespace Raftel.Infrastructure.Data.Configuration.Audit;

/// <summary>
/// Entity Framework configuration for AuditEntry entity.
/// </summary>
internal class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => (Guid)id,
                value => new AuditEntryId(value))
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(2000);

        builder.HasMany(x => x.PropertyChanges)
            .WithOne()
            .HasForeignKey(x => x.AuditEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.EntityName, x.EntityId })
            .HasDatabaseName("IX_AuditEntries_Entity");

        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_AuditEntries_Timestamp");
    }
}