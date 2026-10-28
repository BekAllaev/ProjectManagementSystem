using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.ReadModels;

/// <summary>
/// Row of the list of tasks and header of a task (<c>TaskListItem</c>, <c>TaskDetails</c>, <c>MyTaskItem</c>).
/// </summary>
public sealed class TaskItemView
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public Guid WorkPackageId { get; set; }

    public string WorkPackageDescription { get; set; } = string.Empty;

    /// <summary>
    /// Also allowed to change the status of the task.
    /// </summary>
    public Guid WorkPackageResponsiblePersonId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ResponsiblePersonId { get; set; }

    public ProjectTaskStatus Status { get; set; }

    public decimal EstimatedEffort { get; set; }

    public decimal LoggedHours { get; set; }

    public decimal LoggedCost { get; set; }

    /// <summary>
    /// The task is not finished and a finish-to-start predecessor is not completed.
    /// </summary>
    public bool HasUnfinishedFinishToStartPredecessor { get; set; }

    /// <summary>
    /// Version of the work package, which holds the task (see <c>IDomainEvent.AggregateVersion</c>).
    /// </summary>
    public long WorkPackageVersion { get; set; }

    /// <summary>
    /// Version of the time log of the task; 0 before the first entry.
    /// </summary>
    public long TimeLogVersion { get; set; }
}
