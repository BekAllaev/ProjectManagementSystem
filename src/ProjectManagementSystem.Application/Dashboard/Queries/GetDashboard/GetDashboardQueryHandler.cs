using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;

/// <summary>
/// Reads the rows of the tasks and projects of the caller; which projects those are depends on the day.
/// </summary>
public sealed class GetDashboardQueryHandler(
    IReadModelRepository<TaskItemView> tasks,
    IReadModelRepository<MembershipPeriodView> periods,
    IReadModelRepository<ProjectItemView> projects,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<GetDashboardQuery, MyDashboard>
{
    public async Task<MyDashboard> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.EmployeeId;

        var openTasks = await tasks.ListAsync(
            t => t.ResponsiblePersonId == me && (t.Status == ProjectTaskStatus.Open || t.Status == ProjectTaskStatus.InProgress),
            cancellationToken);
        var projectIds = (await periods.ListOnAsync(timeProvider.Today(), me, cancellationToken)).Select(p => p.ProjectId).Distinct().ToList();
        var myProjects = (await projects.ListAsync(p => projectIds.Contains(p.Id), cancellationToken))
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var taskItems = openTasks
            .OrderBy(t => t.Status == ProjectTaskStatus.InProgress ? 0 : 1)
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(t => new MyTaskItem(
                t.Id, t.Name, t.ProjectId, t.ProjectTitle, t.WorkPackageId, t.WorkPackageDescription, t.Status,
                t.EstimatedEffort, t.LoggedHours, t.WorkPackageVersion))
            .ToList();

        var alerts = myProjects
            .Where(p => p.IsOverspent || p.UsedPercent >= ReadModelRules.FundingAlertPercent)
            .OrderByDescending(p => p.IsOverspent)
            .ThenByDescending(p => p.UsedPercent)
            .Select(p => new FundingAlertItem(
                p.Id, p.Title, p.Currency, p.TotalFunding, p.ActualCost, p.RemainingFunding, p.UsedPercent, p.IsOverspent))
            .ToList();

        return new MyDashboard(
            taskItems,
            myProjects.Select(p => new MyProjectItem(p.Id, p.Title, p.State, p.ProgressPercent, p.Version)).ToList(),
            alerts);
    }
}
