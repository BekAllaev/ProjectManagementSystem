using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.Milestones;

/// <summary>
/// A task cancelled after it was added stays in the milestone but is not counted; a task that no longer exists in the
/// project is left out.
/// </summary>
internal sealed class GetMilestonesQueryHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetMilestonesQuery, MilestoneList>
{
    public async Task<MilestoneList> Handle(GetMilestonesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var milestones = await dbContext.MilestonesOf(project.Id).ToListAsync(cancellationToken);
        var workPackages = await dbContext.WorkPackagesOf(project.Id).ToListAsync(cancellationToken);
        var tasks = workPackages
            .SelectMany(w => w.Tasks.Select(t => (WorkPackage: w, Task: t)))
            .ToDictionary(x => x.Task.Id);
        var today = timeProvider.Today();

        var items = milestones
            .OrderBy(m => m.TargetDate)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(m => Item(m, tasks, today))
            .ToList();

        return new MilestoneList(items);
    }

    private static MilestoneItem Item(
        Milestone milestone, IReadOnlyDictionary<Guid, (WorkPackage WorkPackage, ProjectTask Task)> tasks, DateOnly today)
    {
        var milestoneTasks = milestone.TaskIds
            .Where(tasks.ContainsKey)
            .Select(id => tasks[id])
            .Select(x => new MilestoneTaskItem(
                x.Task.Id, x.WorkPackage.Id, x.Task.Name, x.WorkPackage.Description, x.Task.Status,
                x.Task.Status != ProjectTaskStatus.Cancelled))
            .ToList();
        var progress = TaskProgress.Of(milestoneTasks.Select(t => t.Status));
        var health = ReadModelRules.HealthOf(milestone.Status, milestone.TargetDate, progress, today);
        var daysLate = health == MilestoneHealth.Late ? today.DayNumber - milestone.TargetDate.DayNumber : 0;

        return new MilestoneItem(
            milestone.Id, milestone.Name, milestone.Description, milestone.TargetDate, milestone.Status, milestone.CompletedAt,
            health, daysLate, progress.Completed, progress.Counted, progress.Percent, milestoneTasks, milestone.Version);
    }
}
