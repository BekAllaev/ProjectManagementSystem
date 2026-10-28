using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;

/// <summary>
/// Whether a milestone is late depends on the day, so its health is decided on the read from the stored status, target
/// date and progress.
/// </summary>
public sealed class GetMilestonesQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<MilestoneItemView> milestones,
    IReadModelRepository<MilestoneTaskItemView> milestoneTasks,
    TimeProvider timeProvider) : IQueryHandler<GetMilestonesQuery, MilestoneList>
{
    public async Task<MilestoneList> Handle(GetMilestonesQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var rows = await milestones.ListAsync(m => m.ProjectId == project.Id, cancellationToken);
        var tasksOf = (await milestoneTasks.ListAsync(t => t.ProjectId == project.Id, cancellationToken)).ToLookup(t => t.MilestoneId);
        var today = timeProvider.Today();

        return new MilestoneList(rows
            .OrderBy(m => m.TargetDate)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(m =>
            {
                var health = ReadModelRules.HealthOf(m.Status, m.TargetDate, new TaskProgress(m.CompletedTasks, m.CountedTasks), today);

                return new MilestoneItem(
                    m.Id, m.Name, m.Description, m.TargetDate, m.Status, m.CompletedAt, health,
                    health == MilestoneHealth.Late ? today.DayNumber - m.TargetDate.DayNumber : 0,
                    m.CompletedTasks, m.CountedTasks, m.ProgressPercent,
                    tasksOf[m.Id]
                        .OrderBy(t => t.Position)
                        .Select(t => new MilestoneTaskItem(t.TaskId, t.WorkPackageId, t.Name, t.WorkPackageDescription, t.Status, t.IsCounted))
                        .ToList(),
                    m.Version);
            })
            .ToList());
    }
}
