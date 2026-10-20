using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Schedule;

/// <summary>
/// Cancelled tasks do not take part in the schedule: their dependencies are removed.
/// </summary>
public sealed class RemoveDependenciesOfCancelledWorkPackage(IRepository<ScheduleNetwork> networks, IUnitOfWork unitOfWork)
    : IDomainEventHandler<WorkPackageCancelled>
{
    public async Task HandleAsync(WorkPackageCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.CancelledTaskIds.Count == 0)
            return;

        var network = await networks.FindAsync(domainEvent.ProjectId, cancellationToken);
        if (network is null)
            return;

        network.RemoveDependenciesOf(domainEvent.CancelledTaskIds);
        networks.Update(network);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
