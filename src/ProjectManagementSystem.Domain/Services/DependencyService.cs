using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Adds dependencies to the schedule network of a project. Both tasks must exist in the project and
/// must not be cancelled; tasks live in work packages, so the rule spans aggregates. A task may be
/// cancelled concurrently, which is handled after the commit by a handler of <c>DependencyAdded</c>.
/// Only <see cref="ScheduleNetwork"/> is changed.
/// </summary>
public sealed class DependencyService
{
    public void AddDependency(
        ScheduleNetwork network,
        WorkPackage predecessorWorkPackage,
        Guid predecessorTaskId,
        WorkPackage successorWorkPackage,
        Guid successorTaskId,
        DependencyType type)
    {
        EnsureCanTakePart(network, predecessorWorkPackage, predecessorTaskId);
        EnsureCanTakePart(network, successorWorkPackage, successorTaskId);

        network.AddDependency(new Dependency(
            predecessorTaskId, predecessorWorkPackage.Id, successorTaskId, successorWorkPackage.Id, type));
    }

    private static void EnsureCanTakePart(ScheduleNetwork network, WorkPackage workPackage, Guid taskId)
    {
        if (workPackage.ProjectId != network.ProjectId)
            throw new DomainException($"Work package '{workPackage.Id}' does not belong to project '{network.ProjectId}'.");

        var task = workPackage.GetTask(taskId);
        if (task.Status == ProjectTaskStatus.Cancelled)
            throw new DomainException($"Cancelled task '{task.Name}' cannot take part in a dependency.");
    }
}
