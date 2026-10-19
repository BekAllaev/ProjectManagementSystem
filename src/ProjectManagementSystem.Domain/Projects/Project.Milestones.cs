using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Milestones;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<Milestone> _milestones = [];

    public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();

    public Guid CreateMilestone(string name, string description, DateOnly targetDate)
    {
        var milestone = new Milestone(Guid.NewGuid(), name, description, targetDate);
        _milestones.Add(milestone);

        RaiseDomainEvent(new MilestoneCreated(Id, milestone.Id, milestone.Name, milestone.Description, targetDate));
        return milestone.Id;
    }

    public void AddTaskToMilestone(Guid milestoneId, Guid taskId)
    {
        var milestone = GetMilestone(milestoneId);

        if (FindTask(taskId) is null)
            throw new DomainException($"Task '{taskId}' does not belong to the project.");

        milestone.AddTask(taskId);
        RaiseDomainEvent(new MilestoneTaskAdded(Id, milestone.Id, taskId));
    }

    public void RemoveTaskFromMilestone(Guid milestoneId, Guid taskId)
    {
        var milestone = GetMilestone(milestoneId);

        milestone.RemoveTask(taskId);
        RaiseDomainEvent(new MilestoneTaskRemoved(Id, milestone.Id, taskId));
    }

    public void ChangeMilestoneTargetDate(Guid milestoneId, DateOnly targetDate)
    {
        var milestone = GetMilestone(milestoneId);

        milestone.ChangeTargetDate(targetDate);
        RaiseDomainEvent(new MilestoneTargetDateChanged(Id, milestone.Id, targetDate));
    }

    public void RenameMilestone(Guid milestoneId, string name)
    {
        var milestone = GetMilestone(milestoneId);

        milestone.Rename(name);
        RaiseDomainEvent(new MilestoneRenamed(Id, milestone.Id, milestone.Name));
    }

    public void CompleteMilestone(Guid milestoneId)
    {
        var milestone = GetMilestone(milestoneId);

        milestone.Complete(DateTime.UtcNow);
        RaiseDomainEvent(new MilestoneCompleted(Id, milestone.Id, milestone.CompletedAt!.Value));
    }

    /// <summary>
    /// Allowed at any moment and in any status. Tasks of the milestone are not affected.
    /// </summary>
    public void DeleteMilestone(Guid milestoneId)
    {
        var milestone = GetMilestone(milestoneId);

        _milestones.Remove(milestone);
        RaiseDomainEvent(new MilestoneDeleted(Id, milestone.Id));
    }

    private Milestone GetMilestone(Guid milestoneId) =>
        _milestones.SingleOrDefault(m => m.Id == milestoneId)
        ?? throw new EntityNotFoundException(nameof(Milestone), milestoneId);
}
