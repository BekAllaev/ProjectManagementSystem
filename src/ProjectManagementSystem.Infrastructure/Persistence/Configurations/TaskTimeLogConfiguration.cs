using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.TimeTracking;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class TaskTimeLogConfiguration : IEntityTypeConfiguration<TaskTimeLog>
{
    public void Configure(EntityTypeBuilder<TaskTimeLog> builder)
    {
        builder.ConfigureAggregateRoot("TaskTimeLogs");

        // One log per task: its id is the id of the task.
        builder.Property(l => l.Id).HasColumnName("TaskId");
        builder.Ignore(l => l.TaskId);
        builder.Property(l => l.WorkPackageId);
        builder.Property(l => l.ProjectId);
        builder.HasIndex(l => l.ProjectId);

        builder.OwnsMany(l => l.Entries, entries =>
        {
            entries.ToTable("TimeEntries");
            entries.WithOwner().HasForeignKey("TaskId");

            entries.HasKey(e => e.Id);
            entries.Property(e => e.Id).ValueGeneratedNever();
            entries.Property(e => e.EmployeeId);
            entries.Property(e => e.WorkDate);
            entries.Property(e => e.Hours).HasPrecision(9, 2);
            entries.Property(e => e.HourlyRate).HasPrecision(18, 2);
        });
        builder.Navigation(l => l.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
