using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

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
    /// Tasks are listed in the order they were added.
    /// </summary>
    public DateTime AddedAt { get; set; }
}
