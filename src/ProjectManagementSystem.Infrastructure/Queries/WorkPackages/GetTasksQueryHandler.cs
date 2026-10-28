using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

/// <summary>
/// Tasks come by work package in the order the packages were registered in the project, then by name.
/// </summary>
internal sealed class GetTasksQueryHandler(AppDbContext dbContext, ICurrentUser currentUser) : IQueryHandler<GetTasksQuery, TaskList>
{
    public async Task<TaskList> Handle(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var workPackages = await dbContext.WorkPackagesOf(project.Id).ToListAsync(cancellationToken);
        var network = await dbContext.Network(project.Id).SingleOrDefaultAsync(cancellationToken);
        var registered = project.WorkPackageIds.ToList();

        var tasks = workPackages
            .OrderBy(w => registered.IndexOf(w.Id) is var index and >= 0 ? index : int.MaxValue)
            .SelectMany(w => w.Tasks.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).Select(t => (WorkPackage: w, Task: t)))
            .ToList();
        var statusByTaskId = tasks.ToDictionary(x => x.Task.Id, x => x.Task.Status);
        var dependencies = network?.Dependencies.ToList() ?? [];

        var filtered = tasks
            .Where(x => query.Status is null || x.Task.Status == query.Status)
            .Where(x => query.WorkPackageId is null || x.WorkPackage.Id == query.WorkPackageId)
            .Where(x => query.ResponsiblePersonId is null || x.Task.ResponsiblePersonId == query.ResponsiblePersonId)
            .ToList();

        var taskIds = filtered.Select(x => x.Task.Id).ToList();
        var entries = await dbContext.TimeEntriesOfTasks(taskIds).ToListAsync(cancellationToken);
        var hoursByTaskId = entries.GroupBy(e => e.TaskId).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));
        var employees = await EmployeeDirectory.LoadAsync(dbContext, filtered.Select(x => x.Task.ResponsiblePersonId), cancellationToken);

        var items = filtered
            .Select(x => new TaskListItem(
                x.Task.Id,
                x.Task.Name,
                x.WorkPackage.Id,
                x.WorkPackage.Description,
                employees.Ref(x.Task.ResponsiblePersonId),
                x.Task.Status,
                x.Task.EstimatedEffort,
                hoursByTaskId.GetValueOrDefault(x.Task.Id),
                ReadModelRules.HasUnfinishedFinishToStartPredecessor(x.Task.Id, x.Task.Status, dependencies, statusByTaskId),
                !x.Task.IsFinished && (x.Task.ResponsiblePersonId == currentUser.EmployeeId ||
                                       x.WorkPackage.ResponsiblePersonId == currentUser.EmployeeId),
                x.WorkPackage.Version))
            .ToList();

        return new TaskList(items, ReadModelRules.CountStatuses(tasks.Select(x => x.Task.Status)));
    }
}
