using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.WorkPackages;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(IReadModelRepository<ProjectItemView> projects) : IQueryHandler<GetProjectQuery, ProjectDetails>
{
    public async Task<ProjectDetails> Handle(GetProjectQuery query, CancellationToken cancellationToken)
    {
        var p = await projects.GetAsync(query.ProjectId, cancellationToken);

        return new ProjectDetails(
            p.Id, p.Title, p.Description, p.State, p.IsOpen, p.Currency, p.CreationDate, p.StartDate, p.CompletionDate,
            new TaskStatusCounts(p.OpenTasks, p.InProgressTasks, p.CompletedTasks, p.CancelledTasks),
            p.CountedTasks, p.ProgressPercent, p.Version);
    }
}
