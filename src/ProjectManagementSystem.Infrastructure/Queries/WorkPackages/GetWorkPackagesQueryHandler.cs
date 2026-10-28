using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

/// <summary>
/// Packages come in the order they were registered in the project; a package not registered yet comes last.
/// </summary>
internal sealed class GetWorkPackagesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetWorkPackagesQuery, WorkPackageList>
{
    public async Task<WorkPackageList> Handle(GetWorkPackagesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var workPackages = await dbContext.WorkPackagesOf(project.Id).ToListAsync(cancellationToken);
        var employees = await EmployeeDirectory.LoadAsync(
            dbContext,
            workPackages.Select(w => w.ResponsiblePersonId).Concat(workPackages.SelectMany(w => w.Tasks).Select(t => t.ResponsiblePersonId)),
            cancellationToken);
        var registered = project.WorkPackageIds.ToList();

        var items = workPackages
            .OrderBy(w => registered.IndexOf(w.Id) is var index and >= 0 ? index : int.MaxValue)
            .Select(w =>
            {
                var progress = TaskProgress.Of(w.Tasks.Select(t => t.Status));
                var tasks = w.Tasks
                    .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(t => new WorkPackageTaskItem(t.Id, t.Name, employees.Ref(t.ResponsiblePersonId), t.Status))
                    .ToList();

                return new WorkPackageListItem(
                    w.Id, w.Description, employees.Ref(w.ResponsiblePersonId), w.Status, w.CancellationReason,
                    registered.Contains(w.Id), progress.Completed, progress.Counted, tasks, w.Version);
            })
            .ToList();

        return new WorkPackageList(items);
    }
}
