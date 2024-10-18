using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Data.DbContexts.Localization;

public class TranslationResourceConfiguration : IEntityTypeConfiguration<TranslationResource>
{
    public void Configure(EntityTypeBuilder<TranslationResource> builder)
    {
        builder.ToTable("TranslationResources");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => (TranslationResourceId)value)
            .IsRequired();
        
        builder.Property(r => r.Key)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(r => r.Value)
            .IsRequired();
    }
}