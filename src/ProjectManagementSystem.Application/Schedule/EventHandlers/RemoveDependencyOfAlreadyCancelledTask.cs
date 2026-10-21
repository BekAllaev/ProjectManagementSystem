using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule.EventHandlers;

/// <summary>
/// A task can be cancelled concurrently with adding a dependency to it, after the domain service has
/// checked it. Then the dependency is removed here, as if it had existed before the cancellation.
/// </summary>
public sealed class RemoveDependencyOfAlreadyCancelledTask(
    IRepository<WorkPackage> workPackages,
    IRepository<ScheduleNetwork> networks,
    IUnitOfWork unitOfWork) : IDomainEventHandler<DependencyAdded>
{
    public async Task HandleAsync(DependencyAdded domainEvent, CancellationToken cancellationToken = default)
    {
        var dependency = domainEvent.Dependency;
        var cancelledTaskIds = new List<Guid>();

        if (await IsCancelledAsync(dependency.PredecessorWorkPackageId, dependency.PredecessorTaskId, cancellationToken))
            cancelledTaskIds.Add(dependency.PredecessorTaskId);

        if (await IsCancelledAsync(dependency.SuccessorWorkPackageId, dependency.SuccessorTaskId, cancellationToken))
            cancelledTaskIds.Add(dependency.SuccessorTaskId);

        if (cancelledTaskIds.Count == 0)
            return;

        var network = await networks.FindAsync(domainEvent.ProjectId, cancellationToken);
        if (network is null)
            return;

        network.RemoveDependenciesOf(cancelledTaskIds);
        networks.Update(network);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsCancelledAsync(Guid workPackageId, Guid taskId, CancellationToken cancellationToken)
    {
        var workPackage = await workPackages.FindAsync(workPackageId, cancellationToken);

        return workPackage?.Tasks.SingleOrDefault(t => t.Id == taskId)?.Status == ProjectTaskStatus.Cancelled;
    }
}
