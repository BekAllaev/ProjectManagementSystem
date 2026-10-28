using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.ReadModels;

/// <summary>
/// A task of a milestone (<c>MilestoneTaskItem</c>).
/// </summary>
public sealed class MilestoneTaskItemView
{
    public Guid MilestoneId { get; set; }

    public Guid TaskId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid WorkPackageId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string WorkPackageDescription { get; set; } = string.Empty;

    public ProjectTaskStatus Status { get; set; }

    /// <summary>
    /// Cancelled tasks stay in the milestone but are not counted.
    /// </summary>
    public bool IsCounted { get; set; }

    /// <summary>
    /// 1 for the first task added to the milestone, then one more for every task added; tasks are listed in this order.
    /// </summary>
    public int Position { get; set; }
}
