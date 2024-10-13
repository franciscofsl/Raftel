using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Data.DbContexts.Localization;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => (LanguageId)value)
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.IsoCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.HasMany(l => l.Resources).WithOne();
    }
}