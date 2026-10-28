using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Queries;

/// <summary>
/// Every read of the query handlers, without tracking: the tables of the read models, and the two aggregates without
/// events (<see cref="Employee"/>, <see cref="Risk"/>) and the notifications, which are read as they are stored.
/// Handlers only materialize these queries, so a test checks that SQL Server can translate each of them.
/// </summary>
internal static class ReadModelSources
{
    public static IQueryable<ProjectItemView> ProjectItems(this AppDbContext dbContext, ProjectState? state, string? search)
    {
        var projects = dbContext.Set<ProjectItemView>().AsNoTracking();

        if (state is { } wanted)
            projects = projects.Where(p => p.State == wanted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim().ToLower();
            projects = projects.Where(p => p.Title.ToLower().Contains(text));
        }

        return projects;
    }

    public static IQueryable<ProjectItemView> ProjectItems(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        dbContext.Set<ProjectItemView>().AsNoTracking().Where(p => projectIds.Contains(p.Id));

    public static IQueryable<ProjectItemView> ProjectItem(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<ProjectItemView>().AsNoTracking().Where(p => p.Id == projectId);

    public static IQueryable<RoleItemView> RoleItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<RoleItemView>().AsNoTracking().Where(r => r.ProjectId == projectId);

    public static IQueryable<TeamMemberItemView> TeamMemberItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<TeamMemberItemView>().AsNoTracking().Where(m => m.ProjectId == projectId);

    public static IQueryable<MembershipPeriodView> MembershipPeriodsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<MembershipPeriodView>().AsNoTracking().Where(p => p.ProjectId == projectId);

    /// <summary>
    /// Periods in force on <paramref name="date"/>, of one employee or of everybody.
    /// </summary>
    public static IQueryable<MembershipPeriodView> MembershipPeriodsOn(this AppDbContext dbContext, DateOnly date, Guid? employeeId = null) =>
        dbContext.Set<MembershipPeriodView>().AsNoTracking()
            .Where(p => p.From <= date && (p.To == null || p.To >= date))
            .Where(p => employeeId == null || p.EmployeeId == employeeId);

    public static IQueryable<HourlyRateItemView> HourlyRatesOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<HourlyRateItemView>().AsNoTracking().Where(r => r.ProjectId == projectId);

    public static IQueryable<WorkPackageItemView> WorkPackageItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<WorkPackageItemView>().AsNoTracking().Where(w => w.ProjectId == projectId);

    public static IQueryable<TaskItemView> TaskItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<TaskItemView>().AsNoTracking().Where(t => t.ProjectId == projectId);

    public static IQueryable<TaskItemView> TaskItems(this AppDbContext dbContext, IReadOnlyCollection<Guid> taskIds) =>
        dbContext.Set<TaskItemView>().AsNoTracking().Where(t => taskIds.Contains(t.Id));

    public static IQueryable<TaskItemView> OpenTaskItemsOf(this AppDbContext dbContext, Guid employeeId) =>
        dbContext.Set<TaskItemView>().AsNoTracking()
            .Where(t => t.ResponsiblePersonId == employeeId &&
                        (t.Status == ProjectTaskStatus.Open || t.Status == ProjectTaskStatus.InProgress));

    public static IQueryable<DependencyItemView> DependencyItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<DependencyItemView>().AsNoTracking().Where(d => d.ProjectId == projectId);

    public static IQueryable<DependencyItemView> DependencyItemsOfTask(this AppDbContext dbContext, Guid projectId, Guid taskId) =>
        dbContext.Set<DependencyItemView>().AsNoTracking()
            .Where(d => d.ProjectId == projectId && (d.PredecessorTaskId == taskId || d.SuccessorTaskId == taskId));

    public static IQueryable<TimeEntryItemView> TimeEntryItemsOf(this AppDbContext dbContext, Guid taskId) =>
        dbContext.Set<TimeEntryItemView>().AsNoTracking().Where(e => e.TaskId == taskId);

    public static IQueryable<MilestoneItemView> MilestoneItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<MilestoneItemView>().AsNoTracking().Where(m => m.ProjectId == projectId);

    public static IQueryable<MilestoneTaskItemView> MilestoneTaskItemsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<MilestoneTaskItemView>().AsNoTracking().Where(t => t.ProjectId == projectId);

    public static IQueryable<FundingTrancheItemView> FundingTranchesOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<FundingTrancheItemView>().AsNoTracking().Where(t => t.ProjectId == projectId);

    public static IQueryable<MemberCostItemView> MemberCostsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<MemberCostItemView>().AsNoTracking().Where(c => c.ProjectId == projectId);

    public static IQueryable<WorkPackageCostItemView> WorkPackageCostsOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<WorkPackageCostItemView>().AsNoTracking().Where(c => c.ProjectId == projectId);

    public static IQueryable<Risk> RisksOf(this AppDbContext dbContext, Guid projectId, RiskStatus? status)
    {
        var risks = dbContext.Set<Risk>().AsNoTracking().Where(r => r.ProjectId == projectId);

        return status is { } wanted ? risks.Where(r => r.Status == wanted) : risks;
    }

    public static IQueryable<Employee> Employees(this AppDbContext dbContext, string? search)
    {
        var employees = dbContext.Set<Employee>().AsNoTracking();

        if (string.IsNullOrWhiteSpace(search))
            return employees;

        var text = search.Trim().ToLower();
        return employees.Where(e =>
            e.FirstName.ToLower().Contains(text) || e.LastName.ToLower().Contains(text) || e.NormalizedEmail.Contains(text));
    }

    public static IQueryable<Employee> Employees(this AppDbContext dbContext, IReadOnlyCollection<Guid> employeeIds) =>
        dbContext.Set<Employee>().AsNoTracking().Where(e => employeeIds.Contains(e.Id));

    /// <summary>
    /// Notifications addressed to the employee, and those for the management of <paramref name="managedProjectIds"/>.
    /// Tracked, so the commands that mark them read can change them.
    /// </summary>
    public static IQueryable<NotificationItemView> NotificationsOf(
        this AppDbContext dbContext, Guid employeeId, IReadOnlyCollection<Guid> managedProjectIds) =>
        dbContext.Notifications.Where(n =>
            n.RecipientEmployeeId == employeeId ||
            (n.RecipientEmployeeId == null && managedProjectIds.Contains(n.ProjectId)));

    /// <exception cref="Application.Exceptions.AggregateNotFoundException">The project has no read model (yet).</exception>
    public static async Task<ProjectItemView> GetProjectItemAsync(this AppDbContext dbContext, Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.ProjectItem(projectId).SingleOrDefaultAsync(cancellationToken)
        ?? throw new Application.Exceptions.AggregateNotFoundException(nameof(Project), projectId);
}
