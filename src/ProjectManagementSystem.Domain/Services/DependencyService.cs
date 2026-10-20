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
public sealed class DependencyService(IWorkPackageRepository workPackages, IScheduleNetworkRepository networks)
{
    public async Task AddDependencyAsync(
        Guid projectId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        DependencyType type,
        CancellationToken cancellationToken = default)
    {
        var predecessorWorkPackageId = await GetWorkPackageIdOfTaskAsync(projectId, predecessorTaskId, cancellationToken);
        var successorWorkPackageId = await GetWorkPackageIdOfTaskAsync(projectId, successorTaskId, cancellationToken);

        var network = await networks.FindByProjectIdAsync(projectId, cancellationToken);
        if (network is null)
        {
            network = new ScheduleNetwork(Guid.NewGuid(), projectId);
            networks.Add(network);
        }

        network.AddDependency(new Dependency(
            predecessorTaskId, predecessorWorkPackageId, successorTaskId, successorWorkPackageId, type));
    }

    private async Task<Guid> GetWorkPackageIdOfTaskAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.FindByTaskIdAsync(taskId, cancellationToken);

        if (workPackage is null || workPackage.ProjectId != projectId)
            throw new EntityNotFoundException("Task", taskId);

        var task = workPackage.GetTask(taskId);
        if (task.Status == ProjectTaskStatus.Cancelled)
            throw new DomainException($"Cancelled task '{task.Name}' cannot take part in a dependency.");

        return workPackage.Id;
    }
}
