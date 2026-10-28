using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetProjectsQueryHandler(AppDbContext dbContext) : IQueryHandler<GetProjectsQuery, ProjectList>
{
    public async Task<ProjectList> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var projects = await dbContext.ProjectItems(query.State, query.Search).ToListAsync(cancellationToken);

        return new ProjectList(projects
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new ProjectListItem(
                p.Id, p.Title, p.Description, p.State, p.Currency, p.CreationDate, p.StartDate, p.CompletionDate,
                p.CompletedTasks, p.CountedTasks, p.ProgressPercent,
                p.TotalFunding, p.ActualCost, p.RemainingFunding, p.IsOverspent, p.Version))
            .ToList());
    }
}
