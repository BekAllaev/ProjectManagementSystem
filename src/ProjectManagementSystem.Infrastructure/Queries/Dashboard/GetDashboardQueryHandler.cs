using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.Dashboard;

internal sealed class GetDashboardQueryHandler(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetDashboardQuery, MyDashboard>
{
    public async Task<MyDashboard> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.EmployeeId;

        var memberships = await dbContext.MembershipsOn(timeProvider.Today(), me).ToListAsync(cancellationToken);
        var myProjectIds = memberships.Select(m => m.ProjectId).Distinct().ToList();

        var workPackages = await dbContext.WorkPackagesWithOpenTasksOf(me).ToListAsync(cancellationToken);
        var myTasks = workPackages
            .SelectMany(w => w.Tasks
                .Where(t => t.ResponsiblePersonId == me && !t.IsFinished)
                .Select(t => (WorkPackage: w, Task: t)))
            .ToList();

        var projectIds = myProjectIds.Union(myTasks.Select(x => x.WorkPackage.ProjectId)).ToList();
        var projects = await dbContext.ProjectHeaders(projectIds).ToDictionaryAsync(p => p.Id, cancellationToken);
        var entries = await dbContext.TimeEntriesOfTasks(myTasks.Select(x => x.Task.Id).ToList()).ToListAsync(cancellationToken);
        var progress = await dbContext.TaskProgressAsync(myProjectIds, cancellationToken);
        var funding = await dbContext.FundingFiguresAsync(myProjectIds, cancellationToken);

        var openTasks = myTasks
            .OrderBy(x => x.Task.Status == ProjectTaskStatus.InProgress ? 0 : 1)
            .ThenBy(x => x.Task.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new MyTaskItem(
                x.Task.Id,
                x.Task.Name,
                x.WorkPackage.ProjectId,
                projects.GetValueOrDefault(x.WorkPackage.ProjectId)?.Title ?? string.Empty,
                x.WorkPackage.Id,
                x.WorkPackage.Description,
                x.Task.Status,
                x.Task.EstimatedEffort,
                entries.Where(e => e.TaskId == x.Task.Id).Sum(e => e.Hours),
                x.WorkPackage.Version))
            .ToList();

        var myProjects = myProjectIds
            .Where(projects.ContainsKey)
            .Select(id => projects[id])
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var projectItems = myProjects
            .Select(p => new MyProjectItem(p.Id, p.Title, p.State, progress.GetValueOrDefault(p.Id).Percent, p.Version))
            .ToList();

        var alerts = myProjects
            .Select(p => (Project: p, Figures: funding[p.Id]))
            .Where(x => x.Figures.IsOverspent || x.Figures.UsedPercent >= ReadModelRules.FundingAlertPercent)
            .OrderByDescending(x => x.Figures.IsOverspent)
            .ThenByDescending(x => x.Figures.UsedPercent)
            .Select(x => new FundingAlertItem(
                x.Project.Id, x.Project.Title, x.Project.Currency, x.Figures.Total, x.Figures.ActualCost, x.Figures.Remaining,
                x.Figures.UsedPercent, x.Figures.IsOverspent))
            .ToList();

        return new MyDashboard(openTasks, projectItems, alerts);
    }
}
