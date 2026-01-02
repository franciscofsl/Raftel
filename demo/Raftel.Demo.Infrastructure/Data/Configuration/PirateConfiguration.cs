using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data.Configuration;

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
                bounty => (uint)bounty,
                value => (Bounty)value
            )
            .HasColumnName("Bounty")
            .IsRequired();
         
        builder.Property(typeof(BodyType), "_bodyType")
            .HasField("_bodyType")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("BodyType")
            .IsRequired(); 
        
        builder
            .HasMany<DevilFruit>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EatenDevilFruitsByPirates",
                right => right.HasOne<DevilFruit>()
                    .WithMany()
                    .HasForeignKey("DevilFruitId")
                    .IsRequired(),
                left => left.HasOne<Pirate>()
                    .WithMany()
                    .HasForeignKey("PirateId")
                    .IsRequired()
            );

        // Configure multi-tenancy
        builder.HasTenantId();

        // Configure user auditing
        builder.HasUserAuditing();
    }
}