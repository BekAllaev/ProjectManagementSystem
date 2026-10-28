using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Progress of a set of tasks: cancelled tasks stay in the set but are not counted.
/// </summary>
internal readonly record struct TaskProgress(int Completed, int Counted)
{
    public int Percent => ReadModelRules.Percent(Completed, Counted);

    public static TaskProgress Of(IEnumerable<ProjectTaskStatus> statuses)
    {
        var counted = statuses.Where(s => s != ProjectTaskStatus.Cancelled).ToList();

        return new TaskProgress(counted.Count(s => s == ProjectTaskStatus.Completed), counted.Count);
    }
}

/// <summary>
/// Funding of a project against its actual cost. Funding only grows and may be exceeded.
/// </summary>
internal readonly record struct FundingFigures(decimal Total, decimal ActualCost)
{
    public decimal Remaining => Total - ActualCost;

    public bool IsOverspent => Remaining < 0;

    /// <summary>
    /// Empty without funding, so a project without tranches is not reported as "0% used".
    /// </summary>
    public decimal? UsedPercent => Total == 0 ? null : Math.Round(ActualCost * 100 / Total, 1, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Values the read side computes and the domain does not know (see the read side in <c>docs/domain-model.md</c>).
/// </summary>
internal static class ReadModelRules
{
    /// <summary>
    /// From this share of funding used, the dashboard warns about the project.
    /// </summary>
    public const decimal FundingAlertPercent = 80;

    public static int Percent(int part, int whole) =>
        whole == 0 ? 0 : (int)Math.Round(100m * part / whole, MidpointRounding.AwayFromZero);

    public static TaskStatusCounts CountStatuses(IEnumerable<ProjectTaskStatus> statuses)
    {
        var list = statuses.ToList();

        return new TaskStatusCounts(
            list.Count(s => s == ProjectTaskStatus.Open),
            list.Count(s => s == ProjectTaskStatus.InProgress),
            list.Count(s => s == ProjectTaskStatus.Completed),
            list.Count(s => s == ProjectTaskStatus.Cancelled));
    }

    /// <summary>
    /// The domain lets the responsible person of the task or of its package change the status of an unfinished task.
    /// </summary>
    public static bool CanChangeStatus(TaskItemView task, Guid employeeId) =>
        !IsFinished(task.Status) && (task.ResponsiblePersonId == employeeId || task.WorkPackageResponsiblePersonId == employeeId);

    public static bool IsFinished(ProjectTaskStatus status) =>
        status is ProjectTaskStatus.Completed or ProjectTaskStatus.Cancelled;

    /// <summary>
    /// The warning of the schedule: a finish-to-start predecessor is not completed while the successor is not finished.
    /// Starting the successor is still allowed.
    /// </summary>
    public static bool HasWarning(DependencyType type, ProjectTaskStatus predecessorStatus, ProjectTaskStatus successorStatus) =>
        type == DependencyType.FinishToStart &&
        predecessorStatus != ProjectTaskStatus.Completed &&
        !IsFinished(successorStatus);

    public static bool HasUnfinishedFinishToStartPredecessor(
        Guid taskId,
        ProjectTaskStatus status,
        IEnumerable<Dependency> dependencies,
        IReadOnlyDictionary<Guid, ProjectTaskStatus> statusByTaskId) =>
        dependencies.Any(d =>
            d.SuccessorTaskId == taskId &&
            statusByTaskId.TryGetValue(d.PredecessorTaskId, out var predecessorStatus) &&
            HasWarning(d.Type, predecessorStatus, status));

    /// <summary>
    /// Probability × impact, each counted 1 (low) to 3 (high).
    /// </summary>
    public static int RiskScore(RiskLevel probability, RiskLevel impact) => ((int)probability + 1) * ((int)impact + 1);

    public static RiskLevel RiskLevelOf(int score) => score switch
    {
        >= 6 => RiskLevel.High,
        >= 3 => RiskLevel.Medium,
        _ => RiskLevel.Low,
    };

    public static MilestoneHealth HealthOf(MilestoneStatus status, DateOnly targetDate, TaskProgress progress, DateOnly today) =>
        status switch
        {
            MilestoneStatus.Open when targetDate < today => MilestoneHealth.Late,
            MilestoneStatus.Open => MilestoneHealth.Open,
            _ when progress.Counted == 0 => MilestoneHealth.CompletedEmpty,
            _ when progress.Completed < progress.Counted => MilestoneHealth.CompletedWithLooseEnds,
            _ => MilestoneHealth.Reached,
        };
}
