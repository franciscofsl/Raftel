using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Domain.Ships;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data.Configuration;

public class ShipConfiguration : IEntityTypeConfiguration<Ship>
{
    public void Configure(EntityTypeBuilder<Ship> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new ShipId(value)
            );

        builder.Property(p => p.Name)
            .HasConversion(
                name => (string)name,
                value => (Name)value
            )
            .HasColumnName("Name")
            .IsRequired();

        builder.HasSoftDelete();
    }
}