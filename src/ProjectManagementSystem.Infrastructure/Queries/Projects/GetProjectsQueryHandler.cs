using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetProjectsQueryHandler(AppDbContext dbContext) : IQueryHandler<GetProjectsQuery, ProjectList>
{
    public async Task<ProjectList> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var projects = await dbContext.ProjectHeaders(query.State, query.Search).ToListAsync(cancellationToken);
        var ids = projects.Select(p => p.Id).ToList();

        var progress = await dbContext.TaskProgressAsync(ids, cancellationToken);
        var funding = await dbContext.FundingFiguresAsync(ids, cancellationToken);

        var items = projects
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(p =>
            {
                var tasks = progress.GetValueOrDefault(p.Id);
                var money = funding[p.Id];

                return new ProjectListItem(
                    p.Id, p.Title, p.Description, p.State, p.Currency, p.CreationDate, p.StartDate, p.CompletionDate,
                    tasks.Completed, tasks.Counted, tasks.Percent,
                    money.Total, money.ActualCost, money.Remaining, money.IsOverspent, p.Version);
            })
            .ToList();

        return new ProjectList(items);
    }
}
