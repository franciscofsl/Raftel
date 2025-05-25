using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new RoleId(value)
            );

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasMany<Permission>()
            .WithMany();

        builder.HasTenantId();
    }
} 