using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Data.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,
                value => new UserId(value)
            );

        builder.Property(p => p.Email)
            .HasConversion(
                email => (string)email,
                value => (Email)value
            )
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Surname)
            .HasMaxLength(100);

        builder.Property(_ => _.IdentityUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(x => x.Email);

        // Configure multi-tenancy
        builder.HasTenantId();
    }
}