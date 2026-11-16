using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new PermissionId(value)
            )
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property<RoleId>("RoleId")
            .HasConversion(
                id => (Guid)id,
                value => new RoleId(value)
            )
            .IsRequired();

        builder.HasIndex("RoleId", "Name")
            .IsUnique();

        builder.HasTenantId();
    }
} 