using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Outbox;
using Raftel.Domain.Outbox.ValueObjects;

namespace Raftel.Infrastructure.Data.Configuration.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => (Guid)id,
                value => new OutboxMessageId(value));

        builder.Property(x => x.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(OutboxMessage.MaxErrorLength);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128);

        // The lease doubles as the concurrency token: an outcome is only written by the processor that
        // still holds the lease it claimed with.
        builder.Property(x => x.LockedUntil)
            .IsConcurrencyToken();

        builder.HasIndex(x => x.ProcessedOn)
            .HasDatabaseName("IX_OutboxMessages_ProcessedOn");
    }
}
