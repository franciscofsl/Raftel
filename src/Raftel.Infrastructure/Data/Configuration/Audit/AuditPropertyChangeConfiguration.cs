using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Infrastructure.Data.Audit;

namespace Raftel.Infrastructure.Data.Configuration.Audit;

/// <summary>
/// Entity Framework configuration for AuditPropertyChange entity.
/// </summary>
internal class AuditPropertyChangeConfiguration : IEntityTypeConfiguration<AuditPropertyChange>
{
    public void Configure(EntityTypeBuilder<AuditPropertyChange> builder)
    {
        builder.ToTable("AuditPropertyChanges");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => (Guid)id,
                value => new AuditPropertyChangeId(value))
            .IsRequired();

        builder.Property(x => x.PropertyName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.OldValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.AuditEntryId)
            .HasConversion(
                id => (Guid)id,
                value => new AuditEntryId(value))
            .IsRequired();

        builder.HasIndex(x => x.AuditEntryId)
            .HasDatabaseName("IX_AuditPropertyChanges_AuditEntryId");

        builder.HasIndex(x => x.PropertyName)
            .HasDatabaseName("IX_AuditPropertyChanges_PropertyName");
    }
}