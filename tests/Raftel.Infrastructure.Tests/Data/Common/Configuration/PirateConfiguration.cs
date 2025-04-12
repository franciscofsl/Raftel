using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Infrastructure.Tests.Common.PiratesEntities;
using Raftel.Infrastructure.Tests.Common.PiratesEntities.ValueObjects;

namespace Raftel.Infrastructure.Tests.Data.Common.Configuration;

public class PirateConfiguration : IEntityTypeConfiguration<Pirate>
{
    public void Configure(EntityTypeBuilder<Pirate> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new PirateId(value)
            );

        builder.Property(p => p.Name)
            .HasConversion(
                name => (string)name,
                value => (Name)value
            )
            .HasColumnName("Name")
            .IsRequired();

        builder.Property(p => p.Bounty)
            .HasConversion(
                bounty => (int)bounty,
                value => (Bounty)value
            )
            .HasColumnName("Bounty")
            .IsRequired();
    }
}