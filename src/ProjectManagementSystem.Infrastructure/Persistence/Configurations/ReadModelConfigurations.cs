using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tables of the read models in the schema <see cref="AppDbContext.ReadSchema"/>, named after the read model they hold
/// (<c>ProjectItems</c>, not <c>Projects</c>, so no name repeats a table of the aggregates). Their ids come from the
/// events, never from the database. Indexes follow the queries: almost every read is by project.
/// </summary>
internal static class ReadModelTable
{
    public static void ToReadTable<T>(this EntityTypeBuilder<T> builder, string name) where T : class =>
        builder.ToTable(name, AppDbContext.ReadSchema);

    public static PropertyBuilder<decimal> Money(this PropertyBuilder<decimal> property) => property.HasPrecision(18, 2);

    public static PropertyBuilder<decimal?> Money(this PropertyBuilder<decimal?> property) => property.HasPrecision(18, 2);

    public static PropertyBuilder<decimal> Hours(this PropertyBuilder<decimal> property) => property.HasPrecision(9, 2);
}

internal sealed class ProjectItemViewConfiguration : IEntityTypeConfiguration<ProjectItemView>
{
    public void Configure(EntityTypeBuilder<ProjectItemView> builder)
    {
        builder.ToReadTable("ProjectItems");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Title).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.TotalFunding).Money();
        builder.Property(p => p.ActualCost).Money();
        builder.Property(p => p.RemainingFunding).Money();
        builder.Property(p => p.UsedPercent).HasPrecision(9, 1);
    }
}

internal sealed class RoleItemViewConfiguration : IEntityTypeConfiguration<RoleItemView>
{
    public void Configure(EntityTypeBuilder<RoleItemView> builder)
    {
        builder.ToReadTable("RoleItems");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.HasIndex(r => r.ProjectId);
    }
}

internal sealed class TeamMemberItemViewConfiguration : IEntityTypeConfiguration<TeamMemberItemView>
{
    public void Configure(EntityTypeBuilder<TeamMemberItemView> builder)
    {
        builder.ToReadTable("TeamMemberItems");
        builder.HasKey(m => new { m.ProjectId, m.EmployeeId });
        builder.Property(m => m.RoleName).HasMaxLength(100);
        builder.HasIndex(m => m.EmployeeId);
    }
}

internal sealed class MembershipPeriodViewConfiguration : IEntityTypeConfiguration<MembershipPeriodView>
{
    public void Configure(EntityTypeBuilder<MembershipPeriodView> builder)
    {
        builder.ToReadTable("MembershipPeriods");
        builder.HasKey(p => p.TeamMemberId);
        builder.Property(p => p.TeamMemberId).ValueGeneratedNever();
        builder.HasIndex(p => new { p.ProjectId, p.EmployeeId });
        builder.HasIndex(p => p.EmployeeId);
    }
}

internal sealed class HourlyRateItemViewConfiguration : IEntityTypeConfiguration<HourlyRateItemView>
{
    public void Configure(EntityTypeBuilder<HourlyRateItemView> builder)
    {
        builder.ToReadTable("HourlyRateItems");
        builder.HasKey(r => new { r.TeamMemberId, r.From });
        builder.Property(r => r.Amount).Money();
        builder.HasIndex(r => r.ProjectId);
    }
}

internal sealed class WorkPackageItemViewConfiguration : IEntityTypeConfiguration<WorkPackageItemView>
{
    public void Configure(EntityTypeBuilder<WorkPackageItemView> builder)
    {
        builder.ToReadTable("WorkPackageItems");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(w => w.ProjectId);
    }
}

internal sealed class TaskItemViewConfiguration : IEntityTypeConfiguration<TaskItemView>
{
    public void Configure(EntityTypeBuilder<TaskItemView> builder)
    {
        builder.ToReadTable("TaskItems");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.ProjectTitle).HasMaxLength(200);
        builder.Property(t => t.WorkPackageDescription).HasMaxLength(1000);
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.EstimatedEffort).Hours();
        builder.Property(t => t.LoggedHours).Hours();
        builder.Property(t => t.LoggedCost).Money();
        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.ResponsiblePersonId);
    }
}

internal sealed class DependencyItemViewConfiguration : IEntityTypeConfiguration<DependencyItemView>
{
    public void Configure(EntityTypeBuilder<DependencyItemView> builder)
    {
        builder.ToReadTable("DependencyItems");
        builder.HasKey(d => new { d.ProjectId, d.PredecessorTaskId, d.SuccessorTaskId });
        builder.Property(d => d.PredecessorName).HasMaxLength(200);
        builder.Property(d => d.PredecessorWorkPackageDescription).HasMaxLength(1000);
        builder.Property(d => d.SuccessorName).HasMaxLength(200);
        builder.Property(d => d.SuccessorWorkPackageDescription).HasMaxLength(1000);
    }
}

internal sealed class TimeEntryItemViewConfiguration : IEntityTypeConfiguration<TimeEntryItemView>
{
    public void Configure(EntityTypeBuilder<TimeEntryItemView> builder)
    {
        builder.ToReadTable("TimeEntryItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Hours).Hours();
        builder.Property(e => e.HourlyRate).Money();
        builder.Property(e => e.Cost).Money();
        builder.HasIndex(e => e.TaskId);
        builder.HasIndex(e => e.ProjectId);
    }
}

internal sealed class MilestoneItemViewConfiguration : IEntityTypeConfiguration<MilestoneItemView>
{
    public void Configure(EntityTypeBuilder<MilestoneItemView> builder)
    {
        builder.ToReadTable("MilestoneItems");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.HasIndex(m => m.ProjectId);
    }
}

internal sealed class MilestoneTaskItemViewConfiguration : IEntityTypeConfiguration<MilestoneTaskItemView>
{
    public void Configure(EntityTypeBuilder<MilestoneTaskItemView> builder)
    {
        builder.ToReadTable("MilestoneTaskItems");
        builder.HasKey(t => new { t.MilestoneId, t.TaskId });
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.WorkPackageDescription).HasMaxLength(1000);
        builder.HasIndex(t => t.ProjectId);
    }
}

internal sealed class FundingTrancheItemViewConfiguration : IEntityTypeConfiguration<FundingTrancheItemView>
{
    public void Configure(EntityTypeBuilder<FundingTrancheItemView> builder)
    {
        builder.ToReadTable("FundingTrancheItems");
        builder.HasKey(t => t.EventId);
        builder.Property(t => t.EventId).ValueGeneratedNever();
        builder.Property(t => t.Amount).Money();
        builder.Property(t => t.Reason).HasMaxLength(1000);
        builder.HasIndex(t => t.ProjectId);
    }
}

internal sealed class MemberCostItemViewConfiguration : IEntityTypeConfiguration<MemberCostItemView>
{
    public void Configure(EntityTypeBuilder<MemberCostItemView> builder)
    {
        builder.ToReadTable("MemberCostItems");
        builder.HasKey(c => new { c.ProjectId, c.EmployeeId });
        builder.Property(c => c.Hours).Hours();
        builder.Property(c => c.Cost).Money();
    }
}

internal sealed class WorkPackageCostItemViewConfiguration : IEntityTypeConfiguration<WorkPackageCostItemView>
{
    public void Configure(EntityTypeBuilder<WorkPackageCostItemView> builder)
    {
        builder.ToReadTable("WorkPackageCostItems");
        builder.HasKey(c => c.WorkPackageId);
        builder.Property(c => c.WorkPackageId).ValueGeneratedNever();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Hours).Hours();
        builder.Property(c => c.Cost).Money();
        builder.HasIndex(c => c.ProjectId);
    }
}
