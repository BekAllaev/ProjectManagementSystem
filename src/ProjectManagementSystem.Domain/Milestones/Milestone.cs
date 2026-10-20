using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Milestones.Events;

namespace ProjectManagementSystem.Domain.Milestones;

/// <summary>
/// Checkpoint of the project that refers to tasks from any work packages of the project.
/// Tasks are referenced only by id: the milestone does not check them, and cancelled tasks
/// are ignored on the read side. One task can be in several milestones. After completion
/// the milestone is frozen. Progress, lateness and "closed with unfinished tasks" are computed
/// on the read side.
/// </summary>
public sealed class Milestone : AggregateRoot<Guid>
{
    private readonly HashSet<Guid> _taskIds = [];

    public Guid ProjectId { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public IReadOnlyCollection<Guid> TaskIds => _taskIds;

    public DateOnly TargetDate { get; private set; }

    public MilestoneStatus Status { get; private set; }

    /// <summary>
    /// Empty while the milestone is Open.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    public Milestone(Guid id, Guid projectId, string name, string description, DateOnly targetDate) : base(id)
    {
        ProjectId = projectId;
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description ?? string.Empty;
        TargetDate = targetDate;
        Status = MilestoneStatus.Open;

        RaiseDomainEvent(new MilestoneCreated(ProjectId, Id, Name, Description, TargetDate));
    }

    public void AddTask(Guid taskId)
    {
        EnsureOpen("add a task to");

        if (!_taskIds.Add(taskId))
            throw new DomainException($"Task '{taskId}' is already in milestone '{Name}'.");

        RaiseDomainEvent(new MilestoneTaskAdded(ProjectId, Id, taskId));
    }

    public void RemoveTask(Guid taskId)
    {
        EnsureOpen("remove a task from");

        if (!_taskIds.Remove(taskId))
            throw new DomainException($"Task '{taskId}' is not in milestone '{Name}'.");

        RaiseDomainEvent(new MilestoneTaskRemoved(ProjectId, Id, taskId));
    }

    public void ChangeTargetDate(DateOnly targetDate)
    {
        EnsureOpen("change the target date of");

        TargetDate = targetDate;
        RaiseDomainEvent(new MilestoneTargetDateChanged(ProjectId, Id, targetDate));
    }

    public void Rename(string name)
    {
        EnsureOpen("rename");

        Name = Guard.NotEmpty(name, nameof(Name));
        RaiseDomainEvent(new MilestoneRenamed(ProjectId, Id, Name));
    }

    /// <summary>
    /// Completed manually at any moment, even if not all tasks are done.
    /// </summary>
    public void Complete()
    {
        EnsureOpen("complete");

        Status = MilestoneStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MilestoneCompleted(ProjectId, Id, CompletedAt.Value));
    }

    /// <summary>
    /// Allowed at any moment and in any status. Tasks of the milestone are not affected.
    /// The repository removes the milestone when it saves this change.
    /// </summary>
    public void Delete() => RaiseDomainEvent(new MilestoneDeleted(ProjectId, Id));

    private void EnsureOpen(string action)
    {
        if (Status != MilestoneStatus.Open)
            throw new DomainException($"Cannot {action} milestone '{Name}' because it is already completed.");
    }
}
