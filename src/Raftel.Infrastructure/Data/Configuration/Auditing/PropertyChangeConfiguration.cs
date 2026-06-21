using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Auditing;
using Raftel.Domain.Auditing.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration.Auditing;

public sealed class PropertyChangeConfiguration : IEntityTypeConfiguration<PropertyChange>
{
    public void Configure(EntityTypeBuilder<PropertyChange> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new PropertyChangeId(value)
            );

        builder.Property(x => x.PropertyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.PropertyType)
            .HasMaxLength(256)
            .IsRequired();
    }
}
