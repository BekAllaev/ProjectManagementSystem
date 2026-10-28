using ProjectManagementSystem.Domain.Milestones;

namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the list of milestones (<c>MilestoneItem</c>); its tasks are in <see cref="MilestoneTaskItemView"/>.
/// </summary>
public sealed class MilestoneItemView
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly TargetDate { get; set; }

    public MilestoneStatus Status { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int CompletedTasks { get; set; }

    /// <summary>
    /// Tasks that are not cancelled.
    /// </summary>
    public int CountedTasks { get; set; }

    public int ProgressPercent { get; set; }

    /// <summary>
    /// Version of the milestone (see <c>IDomainEvent.AggregateVersion</c>).
    /// </summary>
    public long Version { get; set; }
}
