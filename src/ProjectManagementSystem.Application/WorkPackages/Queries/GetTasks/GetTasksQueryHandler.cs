using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;

/// <summary>
/// Tasks come by work package in the order of the project, then by name. Whether the caller may change the status
/// depends on the caller, so it is decided on the read.
/// </summary>
public sealed class GetTasksQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<WorkPackageItemView> workPackages,
    IReadModelRepository<TaskItemView> tasks,
    IReadModelRepository<Employee> employees,
    ICurrentUser currentUser) : IQueryHandler<GetTasksQuery, TaskList>
{
    public async Task<TaskList> Handle(GetTasksQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var order = (await workPackages.ListAsync(w => w.ProjectId == project.Id, cancellationToken))
            .InProjectOrder()
            .Select((w, index) => (w.Id, index))
            .ToDictionary(x => x.Id, x => x.index);
        var rows = await tasks.ListAsync(t => t.ProjectId == project.Id, cancellationToken);

        var selected = rows
            .Where(t => query.Status is null || t.Status == query.Status)
            .Where(t => query.WorkPackageId is null || t.WorkPackageId == query.WorkPackageId)
            .Where(t => query.ResponsiblePersonId is null || t.ResponsiblePersonId == query.ResponsiblePersonId)
            .OrderBy(t => order.GetValueOrDefault(t.WorkPackageId, int.MaxValue))
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var names = await EmployeeDirectory.LoadAsync(employees, selected.Select(t => t.ResponsiblePersonId), cancellationToken);

        return new TaskList(
            selected.Select(t => new TaskListItem(
                t.Id, t.Name, t.WorkPackageId, t.WorkPackageDescription, names.Ref(t.ResponsiblePersonId), t.Status,
                t.EstimatedEffort, t.LoggedHours, t.HasUnfinishedFinishToStartPredecessor,
                ReadModelRules.CanChangeStatus(t, currentUser.EmployeeId), t.WorkPackageVersion)).ToList(),
            new TaskStatusCounts(project.OpenTasks, project.InProgressTasks, project.CompletedTasks, project.CancelledTasks));
    }
}
