using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetProjectQueryHandler(AppDbContext dbContext) : IQueryHandler<GetProjectQuery, ProjectDetails>
{
    public async Task<ProjectDetails> Handle(GetProjectQuery query, CancellationToken cancellationToken)
    {
        var p = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);

        return new ProjectDetails(
            p.Id, p.Title, p.Description, p.State, p.IsOpen, p.Currency, p.CreationDate, p.StartDate, p.CompletionDate,
            new TaskStatusCounts(p.OpenTasks, p.InProgressTasks, p.CompletedTasks, p.CancelledTasks),
            p.CountedTasks, p.ProgressPercent, p.Version);
    }
}
