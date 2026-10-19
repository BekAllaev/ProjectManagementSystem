using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Schedule;
using ProjectManagementSystem.Domain.Projects.WorkPackages;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    public ScheduleNetwork ScheduleNetwork { get; }

    public void AddDependency(Guid predecessorTaskId, Guid successorTaskId, DependencyType type)
    {
        var predecessor = GetTaskForDependency(predecessorTaskId);
        var successor = GetTaskForDependency(successorTaskId);

        var dependency = new Dependency(
            predecessor.Id, predecessor.WorkPackageId, successor.Id, successor.WorkPackageId, type);
        ScheduleNetwork.AddDependency(dependency);

        RaiseDomainEvent(new DependencyAdded(Id, dependency));
    }

    public void RemoveDependency(Guid predecessorTaskId, Guid successorTaskId)
    {
        var dependency = ScheduleNetwork.RemoveDependency(predecessorTaskId, successorTaskId);
        RaiseDomainEvent(new DependencyRemoved(Id, dependency));
    }

    /// <summary>
    /// Called in the same operation when a work package or a task is cancelled.
    /// Successors of cancelled tasks get <see cref="PredecessorCancelled"/>, so their
    /// responsible persons can be notified.
    /// </summary>
    private void RemoveDependenciesOf(IReadOnlyCollection<Guid> cancelledTaskIds)
    {
        if (cancelledTaskIds.Count == 0)
            return;

        foreach (var dependency in ScheduleNetwork.RemoveDependenciesOf(cancelledTaskIds))
        {
            RaiseDomainEvent(new DependencyRemoved(Id, dependency));

            if (cancelledTaskIds.Contains(dependency.PredecessorTaskId) && !cancelledTaskIds.Contains(dependency.SuccessorTaskId))
                RaiseDomainEvent(new PredecessorCancelled(Id, dependency));
        }
    }

    private ProjectTask GetTaskForDependency(Guid taskId)
    {
        var task = FindTask(taskId) ?? throw new EntityNotFoundException("Task", taskId);

        if (task.Status == ProjectTaskStatus.Cancelled)
            throw new DomainException($"Cancelled task '{task.Name}' cannot take part in a dependency.");

        return task;
    }

    private ProjectTask? FindTask(Guid taskId) =>
        _workPackages.SelectMany(wp => wp.Tasks).SingleOrDefault(t => t.Id == taskId);
}
