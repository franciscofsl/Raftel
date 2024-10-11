using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Demo.Core.Samples;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Data.Configuration.Samples;

[ExcludeFromCodeCoverage]
public class SamplesConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        builder.ToTable("Samples");

        builder.HasKey(_ => _.Id);
        builder.Property(_ => _.Id)
            .HasConversion(_ => _.Value, _ => (SampleId)_)
            .IsRequired();
    }
}