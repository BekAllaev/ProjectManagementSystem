using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ConfigureAggregateRoot("Milestones");

        builder.Property(m => m.ProjectId);
        builder.Property(m => m.Name).HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.TargetDate);
        builder.Property(m => m.Status);
        builder.Property(m => m.CompletedAt);
        builder.HasIndex(m => m.ProjectId);

        builder.PrimitiveCollection(m => m.TaskIds)
            .HasField("_taskIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
