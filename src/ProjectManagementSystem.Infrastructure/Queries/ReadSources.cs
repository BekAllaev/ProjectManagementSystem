using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Queries;

internal sealed record ProjectHeaderRow(
    Guid Id,
    string Title,
    string Description,
    ProjectState State,
    string Currency,
    DateTime CreationDate,
    DateTime? StartDate,
    DateTime? CompletionDate,
    long Version);

internal sealed record ProjectTitleRow(Guid Id, string Title, string Currency);

internal sealed record TaskStatusRow(Guid ProjectId, ProjectTaskStatus Status);

internal sealed record TrancheRow(Guid ProjectId, decimal Amount);

internal sealed record TimeEntryRow(
    Guid ProjectId,
    Guid WorkPackageId,
    Guid TaskId,
    Guid EmployeeId,
    decimal Hours,
    decimal HourlyRate);

internal sealed record MembershipRow(Guid ProjectId, string ProjectTitle, Guid EmployeeId);

/// <summary>
/// Every read of the query handlers, as queries without tracking. Handlers only materialize them, so all the SQL of the
/// read side is here and a test checks that SQL Server can translate each of them. Whole aggregates are read where a
/// read model needs most of them (one project, its work packages); sums over many projects are read as narrow rows,
/// summed in memory. Rows of owned collections use <c>SelectMany</c> with a result selector, which becomes a join
/// rather than <c>APPLY</c>.
/// </summary>
internal static class ReadSources
{
    public static IQueryable<Project> Project(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<Project>().AsNoTracking().Where(p => p.Id == projectId);

    public static IQueryable<ProjectHeaderRow> ProjectHeaders(this AppDbContext dbContext, ProjectState? state, string? search)
    {
        var projects = dbContext.Set<Project>().AsNoTracking();

        if (state is { } wanted)
            projects = projects.Where(p => p.State == wanted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim().ToLower();
            projects = projects.Where(p => p.Title.ToLower().Contains(text));
        }

        return Headers(projects);
    }

    public static IQueryable<ProjectHeaderRow> ProjectHeaders(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        Headers(dbContext.Set<Project>().AsNoTracking().Where(p => projectIds.Contains(p.Id)));

    // A filter after this projection cannot be translated: filter the projects first.
    private static IQueryable<ProjectHeaderRow> Headers(IQueryable<Project> projects) =>
        projects.Select(p => new ProjectHeaderRow(
            p.Id, p.Title, p.Description, p.State, p.Currency.Code, p.CreationDate, p.StartDate, p.CompletionDate, p.Version));

    public static IQueryable<ProjectTitleRow> ProjectTitles(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        dbContext.Set<Project>().AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new ProjectTitleRow(p.Id, p.Title, p.Currency.Code));

    public static IQueryable<WorkPackage> WorkPackagesOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<WorkPackage>().AsNoTracking().Where(w => w.ProjectId == projectId);

    public static IQueryable<WorkPackage> WorkPackages(this AppDbContext dbContext, IReadOnlyCollection<Guid> workPackageIds) =>
        dbContext.Set<WorkPackage>().AsNoTracking().Where(w => workPackageIds.Contains(w.Id));

    public static IQueryable<WorkPackage> WorkPackagesWithOpenTasksOf(this AppDbContext dbContext, Guid employeeId) =>
        dbContext.Set<WorkPackage>().AsNoTracking()
            .Where(w => w.Tasks.Any(t =>
                t.ResponsiblePersonId == employeeId &&
                (t.Status == ProjectTaskStatus.Open || t.Status == ProjectTaskStatus.InProgress)));

    public static IQueryable<TaskStatusRow> TaskStatusesOf(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        dbContext.Set<WorkPackage>().AsNoTracking()
            .Where(w => projectIds.Contains(w.ProjectId))
            .SelectMany(w => w.Tasks, (w, t) => new TaskStatusRow(w.ProjectId, t.Status));

    public static IQueryable<ProjectFunding> Funding(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<ProjectFunding>().AsNoTracking().Where(f => f.Id == projectId);

    public static IQueryable<TrancheRow> TranchesOf(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        dbContext.Set<ProjectFunding>().AsNoTracking()
            .Where(f => projectIds.Contains(f.Id))
            .SelectMany(f => f.Tranches, (f, t) => new TrancheRow(f.Id, t.Amount));

    public static IQueryable<TimeEntryRow> TimeEntriesOfProjects(this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds) =>
        dbContext.Set<TaskTimeLog>().AsNoTracking()
            .Where(l => projectIds.Contains(l.ProjectId))
            .SelectMany(l => l.Entries, (l, e) => new TimeEntryRow(l.ProjectId, l.WorkPackageId, l.Id, e.EmployeeId, e.Hours, e.HourlyRate));

    public static IQueryable<TimeEntryRow> TimeEntriesOfTasks(this AppDbContext dbContext, IReadOnlyCollection<Guid> taskIds) =>
        dbContext.Set<TaskTimeLog>().AsNoTracking()
            .Where(l => taskIds.Contains(l.Id))
            .SelectMany(l => l.Entries, (l, e) => new TimeEntryRow(l.ProjectId, l.WorkPackageId, l.Id, e.EmployeeId, e.Hours, e.HourlyRate));

    public static IQueryable<TaskTimeLog> TimeLog(this AppDbContext dbContext, Guid taskId) =>
        dbContext.Set<TaskTimeLog>().AsNoTracking().Where(l => l.Id == taskId);

    public static IQueryable<ScheduleNetwork> Network(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<ScheduleNetwork>().AsNoTracking().Where(n => n.Id == projectId);

    public static IQueryable<Milestone> MilestonesOf(this AppDbContext dbContext, Guid projectId) =>
        dbContext.Set<Milestone>().AsNoTracking().Where(m => m.ProjectId == projectId);

    /// <summary>
    /// Memberships in force on <paramref name="date"/>, of one employee or of everybody.
    /// </summary>
    public static IQueryable<MembershipRow> MembershipsOn(this AppDbContext dbContext, DateOnly date, Guid? employeeId = null) =>
        dbContext.Set<Project>().AsNoTracking()
            .SelectMany(p => p.TeamMembers, (p, m) => new { Project = p, Member = m })
            .Where(x => x.Member.From <= date && (x.Member.To == null || x.Member.To >= date))
            .Where(x => employeeId == null || x.Member.EmployeeId == employeeId)
            .Select(x => new MembershipRow(x.Project.Id, x.Project.Title, x.Member.EmployeeId));
}
