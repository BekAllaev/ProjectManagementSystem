using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Operations on <see cref="ScheduleNetwork"/> whose rules need the project and the work packages:
/// adding dependencies to the network of an open project. Both tasks must exist in the project and
/// must not be cancelled; tasks live in work packages, so the rule spans aggregates. A task may be
/// cancelled concurrently, which is handled after the commit by a handler of <c>DependencyAdded</c>.
/// The project may be closed concurrently; this is accepted without compensation, at worst a superfluous
/// dependency appears. Only <see cref="ScheduleNetwork"/> is changed.
/// </summary>
public sealed class ScheduleNetworkService
{
    public void AddDependency(
        Project project,
        ScheduleNetwork network,
        WorkPackage predecessorWorkPackage,
        Guid predecessorTaskId,
        WorkPackage successorWorkPackage,
        Guid successorTaskId,
        DependencyType type)
    {
        if (network.ProjectId != project.Id)
            throw new DomainValidationException($"Schedule network '{network.Id}' does not belong to project '{project.Id}'.");

        if (!project.IsOpen)
            throw new DomainRuleViolationException($"Project '{project.Id}' is {project.State}; it gets no new dependencies.");

        EnsureCanTakePart(network, predecessorWorkPackage, predecessorTaskId);
        EnsureCanTakePart(network, successorWorkPackage, successorTaskId);

        network.AddDependency(new Dependency(
            predecessorTaskId, predecessorWorkPackage.Id, successorTaskId, successorWorkPackage.Id, type));
    }

    private static void EnsureCanTakePart(ScheduleNetwork network, WorkPackage workPackage, Guid taskId)
    {
        if (workPackage.ProjectId != network.ProjectId)
            throw new DomainValidationException($"Work package '{workPackage.Id}' does not belong to project '{network.ProjectId}'.");

        var task = workPackage.GetTask(taskId);
        if (task.Status == ProjectTaskStatus.Cancelled)
            throw new DomainRuleViolationException($"Cancelled task '{task.Name}' cannot take part in a dependency.");
    }
}
