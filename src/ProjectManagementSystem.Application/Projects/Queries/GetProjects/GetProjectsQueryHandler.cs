using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler(IReadModelRepository<ProjectItemView> projects) : IQueryHandler<GetProjectsQuery, ProjectList>
{
    public async Task<ProjectList> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var state = query.State;
        var text = query.Search?.Trim().ToLower() ?? string.Empty;
        var rows = await projects.ListAsync(
            p => (state == null || p.State == state) && (text == string.Empty || p.Title.ToLower().Contains(text)),
            cancellationToken);

        return new ProjectList(rows
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new ProjectListItem(
                p.Id, p.Title, p.Description, p.State, p.Currency, p.CreationDate, p.StartDate, p.CompletionDate,
                p.CompletedTasks, p.CountedTasks, p.ProgressPercent,
                p.TotalFunding, p.ActualCost, p.RemainingFunding, p.IsOverspent, p.Version))
            .ToList());
    }
}
