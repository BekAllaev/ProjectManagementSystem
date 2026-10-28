using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Dashboard;

/// <summary>
/// Reads the rows of the tasks and projects of the caller; which projects those are depends on the day.
/// </summary>
internal sealed class GetDashboardQueryHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetDashboardQuery, MyDashboard>
{
    public async Task<MyDashboard> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.EmployeeId;

        var tasks = await dbContext.OpenTaskItemsOf(me).ToListAsync(cancellationToken);
        var projectIds = await dbContext.MembershipPeriodsOn(timeProvider.Today(), me)
            .Select(p => p.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var projects = (await dbContext.ProjectItems(projectIds).ToListAsync(cancellationToken))
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var openTasks = tasks
            .OrderBy(t => t.Status == ProjectTaskStatus.InProgress ? 0 : 1)
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(t => new MyTaskItem(
                t.Id, t.Name, t.ProjectId, t.ProjectTitle, t.WorkPackageId, t.WorkPackageDescription, t.Status,
                t.EstimatedEffort, t.LoggedHours, t.WorkPackageVersion))
            .ToList();

        var alerts = projects
            .Where(p => p.IsOverspent || p.UsedPercent >= ReadModelRules.FundingAlertPercent)
            .OrderByDescending(p => p.IsOverspent)
            .ThenByDescending(p => p.UsedPercent)
            .Select(p => new FundingAlertItem(
                p.Id, p.Title, p.Currency, p.TotalFunding, p.ActualCost, p.RemainingFunding, p.UsedPercent, p.IsOverspent))
            .ToList();

        return new MyDashboard(
            openTasks,
            projects.Select(p => new MyProjectItem(p.Id, p.Title, p.State, p.ProgressPercent, p.Version)).ToList(),
            alerts);
    }
}
