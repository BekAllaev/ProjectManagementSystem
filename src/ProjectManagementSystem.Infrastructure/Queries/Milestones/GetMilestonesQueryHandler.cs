using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Milestones;

/// <summary>
/// Whether a milestone is late depends on the day, so its health is decided on the read from the stored status, target
/// date and progress.
/// </summary>
internal sealed class GetMilestonesQueryHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetMilestonesQuery, MilestoneList>
{
    public async Task<MilestoneList> Handle(GetMilestonesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var milestones = await dbContext.MilestoneItemsOf(project.Id).ToListAsync(cancellationToken);
        var tasks = (await dbContext.MilestoneTaskItemsOf(project.Id).ToListAsync(cancellationToken)).ToLookup(t => t.MilestoneId);
        var today = timeProvider.Today();

        return new MilestoneList(milestones
            .OrderBy(m => m.TargetDate)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(m =>
            {
                var health = ReadModelRules.HealthOf(m.Status, m.TargetDate, new TaskProgress(m.CompletedTasks, m.CountedTasks), today);

                return new MilestoneItem(
                    m.Id, m.Name, m.Description, m.TargetDate, m.Status, m.CompletedAt, health,
                    health == MilestoneHealth.Late ? today.DayNumber - m.TargetDate.DayNumber : 0,
                    m.CompletedTasks, m.CountedTasks, m.ProgressPercent,
                    tasks[m.Id]
                        .OrderBy(t => t.Position)
                        .Select(t => new MilestoneTaskItem(t.TaskId, t.WorkPackageId, t.Name, t.WorkPackageDescription, t.Status, t.IsCounted))
                        .ToList(),
                    m.Version);
            })
            .ToList());
    }
}
