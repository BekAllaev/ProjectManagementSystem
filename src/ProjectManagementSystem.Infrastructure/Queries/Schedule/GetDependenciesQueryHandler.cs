using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Schedule;

internal sealed class GetDependenciesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetDependenciesQuery, DependencyList>
{
    public async Task<DependencyList> Handle(GetDependenciesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var dependencies = await dbContext.DependencyItemsOf(project.Id).ToListAsync(cancellationToken);

        return new DependencyList(
            dependencies
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
