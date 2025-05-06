using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Pirates.DevilFruits.ValueObjects;

namespace Raftel.Demo.Infrastructure.Data.Configuration;

public class DevilFruitConfiguration : IEntityTypeConfiguration<DevilFruit>
{
    public void Configure(EntityTypeBuilder<DevilFruit> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => (Guid)id, value => new DevilFruitId(value));

        builder.Property(p => p.Name)
            .HasConversion(name => (string)name, value => (Name)value)
            .HasColumnName("Name")
            .IsRequired();

        builder.Property("_kind")
            .HasColumnName("Kind")
            .HasConversion<int>()
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();
    }
}