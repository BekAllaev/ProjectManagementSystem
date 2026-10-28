using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;

public sealed class GetWorkPackagesQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<WorkPackageItemView> workPackages,
    IReadModelRepository<TaskItemView> tasks,
    IReadModelRepository<Employee> employees) : IQueryHandler<GetWorkPackagesQuery, WorkPackageList>
{
    public async Task<WorkPackageList> Handle(GetWorkPackagesQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var rows = await workPackages.ListAsync(w => w.ProjectId == project.Id, cancellationToken);
        var tasksOf = (await tasks.ListAsync(t => t.ProjectId == project.Id, cancellationToken)).ToLookup(t => t.WorkPackageId);
        var names = await EmployeeDirectory.LoadAsync(
            employees,
            rows.Select(w => w.ResponsiblePersonId).Concat(tasksOf.SelectMany(g => g).Select(t => t.ResponsiblePersonId)),
            cancellationToken);

        return new WorkPackageList(rows
            .InProjectOrder()
            .Select(w => new WorkPackageListItem(
                w.Id, w.Description, names.Ref(w.ResponsiblePersonId), w.Status, w.CancellationReason, w.IsRegistered,
                w.CompletedTasks, w.CountedTasks,
                tasksOf[w.Id]
                    .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(t => new WorkPackageTaskItem(t.Id, t.Name, names.Ref(t.ResponsiblePersonId), t.Status))
                    .ToList(),
                w.Version))
            .ToList());
    }
}
