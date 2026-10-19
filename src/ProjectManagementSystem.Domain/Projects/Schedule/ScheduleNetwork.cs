using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects.Schedule;

/// <summary>
/// Dependencies between tasks of the project, possibly from different work packages.
/// The network is always acyclic.
/// </summary>
public sealed class ScheduleNetwork : Entity<Guid>
{
    private readonly List<Dependency> _dependencies = [];

    public Guid ProjectId { get; }

    public IReadOnlyCollection<Dependency> Dependencies => _dependencies.AsReadOnly();

    internal ScheduleNetwork(Guid id, Guid projectId) : base(id)
    {
        ProjectId = projectId;
    }

    internal void AddDependency(Dependency dependency)
    {
        if (dependency.PredecessorTaskId == dependency.SuccessorTaskId)
            throw new DomainException("Task cannot depend on itself.");

        if (FindDependency(dependency.PredecessorTaskId, dependency.SuccessorTaskId) is not null)
            throw new DomainException("Dependency between these tasks already exists.");

        if (IsReachable(from: dependency.SuccessorTaskId, to: dependency.PredecessorTaskId))
            throw new DomainException("Dependency would create a cycle in the schedule network.");

        _dependencies.Add(dependency);
    }

    internal Dependency RemoveDependency(Guid predecessorTaskId, Guid successorTaskId)
    {
        var dependency = FindDependency(predecessorTaskId, successorTaskId)
            ?? throw new DomainException("Dependency between these tasks does not exist.");

        _dependencies.Remove(dependency);
        return dependency;
    }

    /// <summary>
    /// Removes every dependency in which any of the given tasks takes part.
    /// </summary>
    /// <returns>Removed dependencies.</returns>
    internal IReadOnlyList<Dependency> RemoveDependenciesOf(IReadOnlyCollection<Guid> taskIds)
    {
        var removed = _dependencies
            .Where(d => taskIds.Contains(d.PredecessorTaskId) || taskIds.Contains(d.SuccessorTaskId))
            .ToList();

        foreach (var dependency in removed)
            _dependencies.Remove(dependency);

        return removed;
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
