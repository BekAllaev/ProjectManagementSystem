using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetProjectQueryHandler(AppDbContext dbContext) : IQueryHandler<GetProjectQuery, ProjectDetails>
{
    public async Task<ProjectDetails> Handle(GetProjectQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var statuses = await dbContext.TaskStatusesOf([project.Id]).Select(s => s.Status).ToListAsync(cancellationToken);
        var progress = TaskProgress.Of(statuses);

        return new ProjectDetails(
            project.Id, project.Title, project.Description, project.State, project.IsOpen, project.Currency.Code,
            project.CreationDate, project.StartDate, project.CompletionDate,
            ReadModelRules.CountStatuses(statuses), progress.Counted, progress.Percent, project.Version);
    }
}
