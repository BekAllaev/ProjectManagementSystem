using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.ReadModels;

/// <summary>
/// Recomputes the values a read model of a project takes from other rows: the counters of the tasks, the progress of
/// the work packages and milestones, the statuses and warnings of the dependencies, the logged time and cost, and the
/// funding figures. An event handler changes the rows its event is about and then refreshes the whole project, so
/// every derived value is computed from the current rows in one place, and a repeated delivery of an event changes
/// nothing.
/// </summary>
public sealed class ProjectReadModelRefresher(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<TaskItemView> tasks,
    IReadModelRepository<TimeEntryItemView> timeEntries,
    IReadModelRepository<WorkPackageItemView> workPackages,
    IReadModelRepository<DependencyItemView> dependencies,
    IReadModelRepository<MilestoneTaskItemView> milestoneTasks,
    IReadModelRepository<MilestoneItemView> milestones,
    IReadModelRepository<FundingTrancheItemView> tranches,
    IReadModelRepository<MemberCostItemView> memberCosts,
    IReadModelRepository<WorkPackageCostItemView> workPackageCosts)
{
    public async Task RefreshAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return;

        var projectTasks = await tasks.ListAsync(t => t.ProjectId == projectId, cancellationToken);
        var entries = await timeEntries.ListAsync(e => e.ProjectId == projectId, cancellationToken);
        var projectWorkPackages = await workPackages.ListAsync(w => w.ProjectId == projectId, cancellationToken);

        RefreshTime(projectTasks, entries);
        await RefreshScheduleAsync(projectId, projectTasks, cancellationToken);
        RefreshWorkPackages(projectWorkPackages, projectTasks);
        await RefreshMilestonesAsync(projectId, projectTasks, cancellationToken);
        RefreshTaskCounters(project, projectTasks);
        await RefreshCostsAsync(project, entries, projectWorkPackages, cancellationToken);
    }

    private static void RefreshTime(List<TaskItemView> projectTasks, List<TimeEntryItemView> entries)
    {
        foreach (var task in projectTasks)
        {
            var own = entries.Where(e => e.TaskId == task.Id).ToList();
            task.LoggedHours = own.Sum(e => e.Hours);
            task.LoggedCost = own.Sum(e => e.Cost);
        }
    }

    private async Task RefreshScheduleAsync(Guid projectId, List<TaskItemView> projectTasks, CancellationToken cancellationToken)
    {
        var statusById = projectTasks.ToDictionary(t => t.Id, t => t.Status);
        var projectDependencies = await dependencies.ListAsync(d => d.ProjectId == projectId, cancellationToken);

        foreach (var dependency in projectDependencies)
        {
            dependency.PredecessorStatus = statusById.GetValueOrDefault(dependency.PredecessorTaskId, dependency.PredecessorStatus);
            dependency.SuccessorStatus = statusById.GetValueOrDefault(dependency.SuccessorTaskId, dependency.SuccessorStatus);
            dependency.HasWarning = ReadModelRules.HasWarning(dependency.Type, dependency.PredecessorStatus, dependency.SuccessorStatus);
        }

        foreach (var task in projectTasks)
            task.HasUnfinishedFinishToStartPredecessor = projectDependencies.Any(d => d.SuccessorTaskId == task.Id && d.HasWarning);
    }

    private static void RefreshWorkPackages(List<WorkPackageItemView> projectWorkPackages, List<TaskItemView> projectTasks)
    {
        foreach (var workPackage in projectWorkPackages)
        {
            var progress = TaskProgress.Of(projectTasks.Where(t => t.WorkPackageId == workPackage.Id).Select(t => t.Status));
            workPackage.CompletedTasks = progress.Completed;
            workPackage.CountedTasks = progress.Counted;
        }
    }

    private async Task RefreshMilestonesAsync(Guid projectId, List<TaskItemView> projectTasks, CancellationToken cancellationToken)
    {
        var statusById = projectTasks.ToDictionary(t => t.Id, t => t.Status);
        var projectMilestoneTasks = await milestoneTasks.ListAsync(t => t.ProjectId == projectId, cancellationToken);
        var projectMilestones = await milestones.ListAsync(m => m.ProjectId == projectId, cancellationToken);

        foreach (var milestoneTask in projectMilestoneTasks)
        {
            milestoneTask.Status = statusById.GetValueOrDefault(milestoneTask.TaskId, milestoneTask.Status);
            milestoneTask.IsCounted = milestoneTask.Status != ProjectTaskStatus.Cancelled;
        }

        foreach (var milestone in projectMilestones)
        {
            var progress = TaskProgress.Of(projectMilestoneTasks.Where(t => t.MilestoneId == milestone.Id).Select(t => t.Status));
            milestone.CompletedTasks = progress.Completed;
            milestone.CountedTasks = progress.Counted;
            milestone.ProgressPercent = progress.Percent;
        }
    }

    private static void RefreshTaskCounters(ProjectItemView project, List<TaskItemView> projectTasks)
    {
        var counts = ReadModelRules.CountStatuses(projectTasks.Select(t => t.Status));
        var progress = TaskProgress.Of(projectTasks.Select(t => t.Status));

        project.OpenTasks = counts.Open;
        project.InProgressTasks = counts.InProgress;
        project.CompletedTasks = counts.Completed;
        project.CancelledTasks = counts.Cancelled;
        project.CountedTasks = progress.Counted;
        project.ProgressPercent = progress.Percent;
    }

    private async Task RefreshCostsAsync(
        ProjectItemView project, List<TimeEntryItemView> entries, List<WorkPackageItemView> projectWorkPackages, CancellationToken cancellationToken)
    {
        var projectTranches = await tranches.ListAsync(t => t.ProjectId == project.Id, cancellationToken);
        var figures = new FundingFigures(projectTranches.Sum(t => t.Amount), entries.Sum(e => e.Cost));

        project.TotalFunding = figures.Total;
        project.ActualCost = figures.ActualCost;
        project.RemainingFunding = figures.Remaining;
        project.UsedPercent = figures.UsedPercent;
        project.IsOverspent = figures.IsOverspent;

        Sync(
            memberCosts,
            await memberCosts.ListAsync(c => c.ProjectId == project.Id, cancellationToken),
            entries.GroupBy(e => e.EmployeeId).ToDictionary(g => g.Key, g => (Hours: g.Sum(e => e.Hours), Cost: g.Sum(e => e.Cost))),
            c => c.EmployeeId,
            id => new MemberCostItemView { ProjectId = project.Id, EmployeeId = id },
            (row, sums) => (row.Hours, row.Cost) = sums);

        var descriptions = projectWorkPackages.ToDictionary(w => w.Id, w => w.Description);
        Sync(
            workPackageCosts,
            await workPackageCosts.ListAsync(c => c.ProjectId == project.Id, cancellationToken),
            entries.GroupBy(e => e.WorkPackageId).ToDictionary(g => g.Key, g => (Hours: g.Sum(e => e.Hours), Cost: g.Sum(e => e.Cost))),
            c => c.WorkPackageId,
            id => new WorkPackageCostItemView { ProjectId = project.Id, WorkPackageId = id },
            (row, sums) =>
            {
                (row.Hours, row.Cost) = sums;
                row.Description = descriptions.GetValueOrDefault(row.WorkPackageId, row.Description);
            });
    }

    /// <summary>
    /// Makes the rows match the wanted values by key: updates the kept ones, adds the missing ones, removes the rest.
    /// </summary>
    private static void Sync<TRow, TValue>(
        IReadModelRepository<TRow> repository,
        List<TRow> rows,
        Dictionary<Guid, TValue> wanted,
        Func<TRow, Guid> keyOf,
        Func<Guid, TRow> create,
        Action<TRow, TValue> update)
        where TRow : class
    {
        foreach (var row in rows.Where(r => !wanted.ContainsKey(keyOf(r))))
            repository.Remove(row);

        var existing = rows.ToDictionary(keyOf);
        foreach (var (key, value) in wanted)
        {
            if (!existing.TryGetValue(key, out var row))
            {
                row = create(key);
                repository.Add(row);
            }

            update(row, value);
        }
    }
}
