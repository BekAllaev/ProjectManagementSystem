using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

internal sealed class GetWorkPackagesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetWorkPackagesQuery, WorkPackageList>
{
    public async Task<WorkPackageList> Handle(GetWorkPackagesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var workPackages = await dbContext.WorkPackageItemsOf(project.Id).ToListAsync(cancellationToken);
        var tasks = (await dbContext.TaskItemsOf(project.Id).ToListAsync(cancellationToken)).ToLookup(t => t.WorkPackageId);
        var employees = await EmployeeDirectory.LoadAsync(
            dbContext,
            workPackages.Select(w => w.ResponsiblePersonId).Concat(tasks.SelectMany(g => g).Select(t => t.ResponsiblePersonId)),
            cancellationToken);

        return new WorkPackageList(workPackages
            .InProjectOrder()
            .Select(w => new WorkPackageListItem(
                w.Id, w.Description, employees.Ref(w.ResponsiblePersonId), w.Status, w.CancellationReason, w.IsRegistered,
                w.CompletedTasks, w.CountedTasks,
                tasks[w.Id]
                    .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(t => new WorkPackageTaskItem(t.Id, t.Name, employees.Ref(t.ResponsiblePersonId), t.Status))
                    .ToList(),
                w.Version))
            .ToList());
    }
}
