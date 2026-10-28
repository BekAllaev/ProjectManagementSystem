using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

/// <summary>
/// Tasks come by work package in the order of the project, then by name. Whether the caller may change the status
/// depends on the caller, so it is decided on the read.
/// </summary>
internal sealed class GetTasksQueryHandler(AppDbContext dbContext, ICurrentUser currentUser) : IQueryHandler<GetTasksQuery, TaskList>
{
    public async Task<TaskList> Handle(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var order = (await dbContext.WorkPackageItemsOf(project.Id).ToListAsync(cancellationToken))
            .InProjectOrder()
            .Select((w, index) => (w.Id, index))
            .ToDictionary(x => x.Id, x => x.index);
        var tasks = await dbContext.TaskItemsOf(project.Id).ToListAsync(cancellationToken);

        var selected = tasks
            .Where(t => query.Status is null || t.Status == query.Status)
            .Where(t => query.WorkPackageId is null || t.WorkPackageId == query.WorkPackageId)
            .Where(t => query.ResponsiblePersonId is null || t.ResponsiblePersonId == query.ResponsiblePersonId)
            .OrderBy(t => order.GetValueOrDefault(t.WorkPackageId, int.MaxValue))
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var employees = await EmployeeDirectory.LoadAsync(dbContext, selected.Select(t => t.ResponsiblePersonId), cancellationToken);

        return new TaskList(
            selected.Select(t => new TaskListItem(
                t.Id, t.Name, t.WorkPackageId, t.WorkPackageDescription, employees.Ref(t.ResponsiblePersonId), t.Status,
                t.EstimatedEffort, t.LoggedHours, t.HasUnfinishedFinishToStartPredecessor,
                ReadModelRules.CanChangeStatus(t, currentUser.EmployeeId), t.WorkPackageVersion)).ToList(),
            new TaskStatusCounts(project.OpenTasks, project.InProgressTasks, project.CompletedTasks, project.CancelledTasks));
    }
}
