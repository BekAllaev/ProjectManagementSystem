using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ParkedEventTypeConfiguration : IEntityTypeConfiguration<ParkedEventType>
{
    public void Configure(EntityTypeBuilder<ParkedEventType> builder)
    {
        builder.ToTable("ParkedEventTypes");

        builder.HasKey(p => p.EventType);
        builder.Property(p => p.EventType).HasMaxLength(200);
        builder.Property(p => p.Reason);
    }
}
