using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Domain.Outbox;
using Raftel.Domain.Outbox.ValueObjects;
using Raftel.Infrastructure.Outbox;

namespace Raftel.Infrastructure.Data.Configuration.Outbox;

internal sealed class OutboxHandlerReceiptConfiguration : IEntityTypeConfiguration<OutboxHandlerReceipt>
{
    public void Configure(EntityTypeBuilder<OutboxHandlerReceipt> builder)
    {
        builder.ToTable("OutboxHandlerReceipts");

        builder.HasKey(x => new { x.MessageId, x.Handler });

        builder.Property(x => x.MessageId)
            .HasConversion(
                id => (Guid)id,
                value => new OutboxMessageId(value));

        builder.Property(x => x.Handler)
            .HasMaxLength(OutboxHandlerReceipt.MaxHandlerLength)
            .IsRequired();

        builder.Property(x => x.CompletedOn)
            .IsRequired();

        builder.HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
