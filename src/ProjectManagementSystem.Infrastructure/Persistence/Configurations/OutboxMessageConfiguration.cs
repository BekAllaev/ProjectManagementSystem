using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Sequence).IsClustered();
        builder.Property(m => m.Sequence).UseIdentityColumn();

        builder.Property(m => m.EventType).HasMaxLength(200);
        builder.Property(m => m.Payload);
        builder.Property(m => m.LastError);

        builder.HasIndex(m => m.EventId).IsUnique();

        // The processor reads the earliest message that is neither processed nor parked.
        builder.HasIndex(m => new { m.ProcessedAt, m.ParkedAt, m.Sequence });
    }
}
