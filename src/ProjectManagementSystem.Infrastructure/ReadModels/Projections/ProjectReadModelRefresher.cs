using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Recomputes the values a read model of a project takes from other rows: the counters of the tasks, the progress of
/// the work packages and milestones, the statuses and warnings of the dependencies, the logged time and cost, and the
/// funding figures. A projection changes the rows its event is about and then refreshes the whole project, so every
/// derived value is computed from the current rows in one place, and a repeated delivery of an event changes nothing.
/// </summary>
internal sealed class ProjectReadModelRefresher(ReadModelStore store)
{
    public async Task RefreshAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await store.FindAsync<ProjectItemView>(cancellationToken, projectId);
        if (project is null)
            return;

        var tasks = await store.LoadAsync<TaskItemView>(t => t.ProjectId == projectId, cancellationToken);
        var entries = await store.LoadAsync<TimeEntryItemView>(e => e.ProjectId == projectId, cancellationToken);
        var workPackages = await store.LoadAsync<WorkPackageItemView>(w => w.ProjectId == projectId, cancellationToken);

        RefreshTime(tasks, entries);
        await RefreshScheduleAsync(projectId, tasks, cancellationToken);
        RefreshWorkPackages(workPackages, tasks);
        await RefreshMilestonesAsync(projectId, tasks, cancellationToken);
        RefreshTaskCounters(project, tasks);
        await RefreshCostsAsync(project, entries, workPackages, cancellationToken);
    }

    private static void RefreshTime(List<TaskItemView> tasks, List<TimeEntryItemView> entries)
    {
        foreach (var task in tasks)
        {
            var own = entries.Where(e => e.TaskId == task.Id).ToList();
            task.LoggedHours = own.Sum(e => e.Hours);
            task.LoggedCost = own.Sum(e => e.Cost);
        }
    }

    private async Task RefreshScheduleAsync(Guid projectId, List<TaskItemView> tasks, CancellationToken cancellationToken)
    {
        var statusById = tasks.ToDictionary(t => t.Id, t => t.Status);
        var dependencies = await store.LoadAsync<DependencyItemView>(d => d.ProjectId == projectId, cancellationToken);

        foreach (var dependency in dependencies)
        {
            dependency.PredecessorStatus = statusById.GetValueOrDefault(dependency.PredecessorTaskId, dependency.PredecessorStatus);
            dependency.SuccessorStatus = statusById.GetValueOrDefault(dependency.SuccessorTaskId, dependency.SuccessorStatus);
            dependency.HasWarning = ReadModelRules.HasWarning(dependency.Type, dependency.PredecessorStatus, dependency.SuccessorStatus);
        }

        foreach (var task in tasks)
            task.HasUnfinishedFinishToStartPredecessor = dependencies.Any(d => d.SuccessorTaskId == task.Id && d.HasWarning);
    }

    private static void RefreshWorkPackages(List<WorkPackageItemView> workPackages, List<TaskItemView> tasks)
    {
        foreach (var workPackage in workPackages)
        {
            var progress = TaskProgress.Of(tasks.Where(t => t.WorkPackageId == workPackage.Id).Select(t => t.Status));
            workPackage.CompletedTasks = progress.Completed;
            workPackage.CountedTasks = progress.Counted;
        }
    }

    private async Task RefreshMilestonesAsync(Guid projectId, List<TaskItemView> tasks, CancellationToken cancellationToken)
    {
        var statusById = tasks.ToDictionary(t => t.Id, t => t.Status);
        var milestoneTasks = await store.LoadAsync<MilestoneTaskItemView>(t => t.ProjectId == projectId, cancellationToken);
        var milestones = await store.LoadAsync<MilestoneItemView>(m => m.ProjectId == projectId, cancellationToken);

        foreach (var milestoneTask in milestoneTasks)
        {
            milestoneTask.Status = statusById.GetValueOrDefault(milestoneTask.TaskId, milestoneTask.Status);
            milestoneTask.IsCounted = milestoneTask.Status != ProjectTaskStatus.Cancelled;
        }

        foreach (var milestone in milestones)
        {
            var progress = TaskProgress.Of(milestoneTasks.Where(t => t.MilestoneId == milestone.Id).Select(t => t.Status));
            milestone.CompletedTasks = progress.Completed;
            milestone.CountedTasks = progress.Counted;
            milestone.ProgressPercent = progress.Percent;
        }
    }

    private static void RefreshTaskCounters(ProjectItemView project, List<TaskItemView> tasks)
    {
        var counts = ReadModelRules.CountStatuses(tasks.Select(t => t.Status));
        var progress = TaskProgress.Of(tasks.Select(t => t.Status));

        project.OpenTasks = counts.Open;
        project.InProgressTasks = counts.InProgress;
        project.CompletedTasks = counts.Completed;
        project.CancelledTasks = counts.Cancelled;
        project.CountedTasks = progress.Counted;
        project.ProgressPercent = progress.Percent;
    }

    private async Task RefreshCostsAsync(
        ProjectItemView project, List<TimeEntryItemView> entries, List<WorkPackageItemView> workPackages, CancellationToken cancellationToken)
    {
        var tranches = await store.LoadAsync<FundingTrancheItemView>(t => t.ProjectId == project.Id, cancellationToken);
        var figures = new FundingFigures(tranches.Sum(t => t.Amount), entries.Sum(e => e.Cost));

        project.TotalFunding = figures.Total;
        project.ActualCost = figures.ActualCost;
        project.RemainingFunding = figures.Remaining;
        project.UsedPercent = figures.UsedPercent;
        project.IsOverspent = figures.IsOverspent;

        var memberCosts = await store.LoadAsync<MemberCostItemView>(c => c.ProjectId == project.Id, cancellationToken);
        Sync(
            memberCosts,
            entries.GroupBy(e => e.EmployeeId).ToDictionary(g => g.Key, g => (Hours: g.Sum(e => e.Hours), Cost: g.Sum(e => e.Cost))),
            c => c.EmployeeId,
            id => new MemberCostItemView { ProjectId = project.Id, EmployeeId = id },
            (row, sums) => (row.Hours, row.Cost) = sums);

        var descriptions = workPackages.ToDictionary(w => w.Id, w => w.Description);
        var workPackageCosts = await store.LoadAsync<WorkPackageCostItemView>(c => c.ProjectId == project.Id, cancellationToken);
        Sync(
            workPackageCosts,
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
    private void Sync<TRow, TValue>(
        List<TRow> rows,
        Dictionary<Guid, TValue> wanted,
        Func<TRow, Guid> keyOf,
        Func<Guid, TRow> create,
        Action<TRow, TValue> update)
        where TRow : class
    {
        foreach (var row in rows.Where(r => !wanted.ContainsKey(keyOf(r))))
            store.Remove(row);

        var existing = rows.ToDictionary(keyOf);
        foreach (var (key, value) in wanted)
        {
            if (!existing.TryGetValue(key, out var row))
            {
                row = create(key);
                store.Add(row);
            }

            update(row, value);
        }
    }
}
