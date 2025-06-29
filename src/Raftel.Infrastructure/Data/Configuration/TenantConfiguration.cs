using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new TenantId(value)
            );

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasConversion(
                code => (string)code,
                value => (Code)value
            )
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ConnectionString)
            .HasConversion(
                encryptedString => encryptedString != null ? encryptedString.GetEncryptedValue() : null,
                value => value != null ? EncryptedString.FromEncrypted(value, null).Value : null
            )
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
} 