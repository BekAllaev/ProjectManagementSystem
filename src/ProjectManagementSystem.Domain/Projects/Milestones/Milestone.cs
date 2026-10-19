using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects.Milestones;

/// <summary>
/// Checkpoint of the project that refers to tasks from any work packages of the project.
/// One task can be in several milestones. After completion the milestone is frozen.
/// Progress, lateness and "closed with unfinished tasks" are computed on the read side.
/// </summary>
public sealed class Milestone : Entity<Guid>
{
    private readonly HashSet<Guid> _taskIds = [];

    public string Name { get; private set; }

    public string Description { get; private set; }

    public IReadOnlyCollection<Guid> TaskIds => _taskIds;

    public DateOnly TargetDate { get; private set; }

    public MilestoneStatus Status { get; private set; }

    /// <summary>
    /// Empty while the milestone is Open.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    internal Milestone(Guid id, string name, string description, DateOnly targetDate) : base(id)
    {
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description ?? string.Empty;
        TargetDate = targetDate;
        Status = MilestoneStatus.Open;
    }

    internal void AddTask(Guid taskId)
    {
        EnsureOpen("add a task to");

        if (!_taskIds.Add(taskId))
            throw new DomainException($"Task '{taskId}' is already in milestone '{Name}'.");
    }

    internal void RemoveTask(Guid taskId)
    {
        EnsureOpen("remove a task from");

        if (!_taskIds.Remove(taskId))
            throw new DomainException($"Task '{taskId}' is not in milestone '{Name}'.");
    }

    internal void ChangeTargetDate(DateOnly targetDate)
    {
        EnsureOpen("change the target date of");
        TargetDate = targetDate;
    }

    internal void Rename(string name)
    {
        EnsureOpen("rename");
        Name = Guard.NotEmpty(name, nameof(Name));
    }

    /// <summary>
    /// Completed manually at any moment, even if not all tasks are done.
    /// </summary>
    internal void Complete(DateTime completedAt)
    {
        EnsureOpen("complete");

        Status = MilestoneStatus.Completed;
        CompletedAt = completedAt;
    }

    private void EnsureOpen(string action)
    {
        if (Status != MilestoneStatus.Open)
            throw new DomainException($"Cannot {action} milestone '{Name}' because it is already completed.");
    }
}
