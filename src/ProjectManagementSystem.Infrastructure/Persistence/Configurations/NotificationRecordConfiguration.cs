using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class NotificationRecordConfiguration : IEntityTypeConfiguration<NotificationRecord>
{
    public void Configure(EntityTypeBuilder<NotificationRecord> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.IdempotencyKey).HasMaxLength(450);
        builder.Property(n => n.Title).HasMaxLength(200);
        builder.Property(n => n.Message).HasMaxLength(2000);

        builder.HasIndex(n => n.IdempotencyKey).IsUnique();
        builder.HasIndex(n => new { n.ProjectId, n.RecipientEmployeeId });
        builder.HasIndex(n => n.RecipientEmployeeId);
    }
}
