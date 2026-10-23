using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class WorkPackageConfiguration : IEntityTypeConfiguration<WorkPackage>
{
    public void Configure(EntityTypeBuilder<WorkPackage> builder)
    {
        builder.ConfigureAggregateRoot("WorkPackages");

        builder.Property(w => w.ProjectId);
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.ResponsiblePersonId);
        builder.Property(w => w.Status);
        builder.Property(w => w.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(w => w.ProjectId);

        builder.OwnsMany(w => w.Tasks, tasks =>
        {
            tasks.ToTable("Tasks");
            tasks.WithOwner().HasForeignKey(t => t.WorkPackageId);

            tasks.HasKey(t => t.Id);
            tasks.Property(t => t.Id).ValueGeneratedNever();
            tasks.Property(t => t.Name).HasMaxLength(200);
            tasks.Property(t => t.Description).HasMaxLength(2000);
            tasks.Property(t => t.ResponsiblePersonId);
            tasks.Property(t => t.Status);
            tasks.Property(t => t.EstimatedEffort).HasPrecision(9, 2);
            tasks.Ignore(t => t.IsFinished);
        });
        builder.Navigation(w => w.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
