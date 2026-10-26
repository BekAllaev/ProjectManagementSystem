using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule.Events;

namespace ProjectManagementSystem.Domain.Schedule;

/// <summary>
/// Dependencies between tasks of one project, possibly from different work packages.
/// There is one network per project, so its id is the id of the project. The network is always acyclic.
/// Tasks live in other aggregates, so a new dependency is added through
/// <see cref="Services.ScheduleNetworkService"/>, which checks that both tasks exist and are not cancelled.
/// </summary>
public sealed class ScheduleNetwork : AggregateRoot<Guid>
{
    private readonly List<Dependency> _dependencies = [];

    public Guid ProjectId => Id;

    public IReadOnlyCollection<Dependency> Dependencies => _dependencies.AsReadOnly();

    /// <summary>
    /// For EF Core materialization only; does not raise domain events.
    /// </summary>
    private ScheduleNetwork()
    {
    }

    public ScheduleNetwork(Guid projectId) : base(projectId)
    {
    }

    internal void AddDependency(Dependency dependency)
    {
        if (dependency.PredecessorTaskId == dependency.SuccessorTaskId)
            throw new DomainValidationException("Task cannot depend on itself.");

        if (FindDependency(dependency.PredecessorTaskId, dependency.SuccessorTaskId) is not null)
            throw new DomainRuleViolationException("Dependency between these tasks already exists.");

        if (IsReachable(from: dependency.SuccessorTaskId, to: dependency.PredecessorTaskId))
            throw new DomainRuleViolationException("Dependency would create a cycle in the schedule network.");

        _dependencies.Add(dependency);
        RaiseDomainEvent(new DependencyAdded(ProjectId, dependency));
    }

    public void RemoveDependency(Guid predecessorTaskId, Guid successorTaskId)
    {
        var dependency = FindDependency(predecessorTaskId, successorTaskId)
            ?? throw new DomainRuleViolationException("Dependency between these tasks does not exist.");

        _dependencies.Remove(dependency);
        RaiseDomainEvent(new DependencyRemoved(ProjectId, dependency));
    }

    /// <summary>
    /// Removes every dependency in which any of the given (cancelled) tasks takes part.
    /// Successors of cancelled tasks get <see cref="PredecessorCancelled"/>, so their
    /// responsible persons can be notified. Calling it again for the same tasks changes nothing.
    /// </summary>
    public void RemoveDependenciesOf(IReadOnlyCollection<Guid> cancelledTaskIds)
    {
        var removed = _dependencies
            .Where(d => cancelledTaskIds.Contains(d.PredecessorTaskId) || cancelledTaskIds.Contains(d.SuccessorTaskId))
            .ToList();

        foreach (var dependency in removed)
        {
            _dependencies.Remove(dependency);
            RaiseDomainEvent(new DependencyRemoved(ProjectId, dependency));

            if (cancelledTaskIds.Contains(dependency.PredecessorTaskId) && !cancelledTaskIds.Contains(dependency.SuccessorTaskId))
                RaiseDomainEvent(new PredecessorCancelled(ProjectId, dependency));
        }
    }

    private Dependency? FindDependency(Guid predecessorTaskId, Guid successorTaskId) =>
        _dependencies.SingleOrDefault(d => d.PredecessorTaskId == predecessorTaskId && d.SuccessorTaskId == successorTaskId);

    private bool IsReachable(Guid from, Guid to)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(from);

        while (stack.Count > 0)
        {
            var taskId = stack.Pop();
            if (taskId == to)
                return true;

            if (!visited.Add(taskId))
                continue;

            foreach (var dependency in _dependencies.Where(d => d.PredecessorTaskId == taskId))
                stack.Push(dependency.SuccessorTaskId);
        }

        return false;
    }
}
