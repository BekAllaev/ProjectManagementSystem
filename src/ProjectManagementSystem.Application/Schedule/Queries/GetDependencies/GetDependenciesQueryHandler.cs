using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;

public sealed class GetDependenciesQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<DependencyItemView> dependencies) : IQueryHandler<GetDependenciesQuery, DependencyList>
{
    public async Task<DependencyList> Handle(GetDependenciesQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var rows = await dependencies.ListAsync(d => d.ProjectId == project.Id, cancellationToken);

        return new DependencyList(
            rows
                .OrderBy(d => d.PredecessorName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(d => d.SuccessorName, StringComparer.CurrentCultureIgnoreCase)
                .Select(d => new DependencyItem(
                    new DependencyTaskItem(
                        d.PredecessorTaskId, d.PredecessorWorkPackageId, d.PredecessorName, d.PredecessorWorkPackageDescription,
                        d.PredecessorStatus),
                    new DependencyTaskItem(
                        d.SuccessorTaskId, d.SuccessorWorkPackageId, d.SuccessorName, d.SuccessorWorkPackageDescription, d.SuccessorStatus),
                    d.Type,
                    d.IsAcrossWorkPackages,
                    d.HasWarning))
                .ToList(),
            project.ScheduleVersion);
    }
}
