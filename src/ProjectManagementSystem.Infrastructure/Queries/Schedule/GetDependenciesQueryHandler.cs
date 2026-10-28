using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Schedule;

/// <summary>
/// A dependency whose task no longer exists in its work package is left out.
/// </summary>
internal sealed class GetDependenciesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetDependenciesQuery, DependencyList>
{
    public async Task<DependencyList> Handle(GetDependenciesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var network = await dbContext.Network(project.Id).SingleOrDefaultAsync(cancellationToken);
        if (network is null)
            return new DependencyList([], 0);

        var workPackages = await dbContext.WorkPackagesOf(project.Id).ToDictionaryAsync(w => w.Id, cancellationToken);

        DependencyTaskItem? Item(Guid taskId, Guid workPackageId)
        {
            if (!workPackages.TryGetValue(workPackageId, out var workPackage) || !workPackage.ContainsTask(taskId))
                return null;

            var task = workPackage.GetTask(taskId);
            return new DependencyTaskItem(task.Id, workPackage.Id, task.Name, workPackage.Description, task.Status);
        }

        var items = network.Dependencies
            .Select(d => (
                Dependency: d,
                Predecessor: Item(d.PredecessorTaskId, d.PredecessorWorkPackageId),
                Successor: Item(d.SuccessorTaskId, d.SuccessorWorkPackageId)))
            .Where(x => x.Predecessor is not null && x.Successor is not null)
            .Select(x => new DependencyItem(
                x.Predecessor!,
                x.Successor!,
                x.Dependency.Type,
                x.Dependency.PredecessorWorkPackageId != x.Dependency.SuccessorWorkPackageId,
                ReadModelRules.HasWarning(x.Dependency, x.Predecessor!.Status, x.Successor!.Status)))
            .OrderBy(d => d.Predecessor.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(d => d.Successor.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new DependencyList(items, network.Version);
    }
}
