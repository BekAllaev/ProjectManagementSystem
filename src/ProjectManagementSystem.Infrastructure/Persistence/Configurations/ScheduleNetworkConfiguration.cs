using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.Schedule;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ScheduleNetworkConfiguration : IEntityTypeConfiguration<ScheduleNetwork>
{
    public void Configure(EntityTypeBuilder<ScheduleNetwork> builder)
    {
        builder.ConfigureAggregateRoot("ScheduleNetworks");

        // One network per project: its id is the id of the project.
        builder.Property(n => n.Id).HasColumnName("ProjectId");
        builder.Ignore(n => n.ProjectId);

        builder.OwnsMany(n => n.Dependencies, dependencies =>
        {
            dependencies.ToTable("Dependencies");
            dependencies.WithOwner().HasForeignKey("ProjectId");

            // The network holds at most one dependency between two tasks.
            dependencies.HasKey("ProjectId", nameof(Dependency.PredecessorTaskId), nameof(Dependency.SuccessorTaskId));
            dependencies.Property(d => d.PredecessorWorkPackageId);
            dependencies.Property(d => d.SuccessorWorkPackageId);
            dependencies.Property(d => d.Type);
        });
        builder.Navigation(n => n.Dependencies).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
