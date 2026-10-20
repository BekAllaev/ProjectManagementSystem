using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Schedule;

/// <summary>
/// A cancelled task does not take part in the schedule: its dependencies are removed.
/// </summary>
public sealed class RemoveDependenciesOfCancelledTask(IScheduleNetworkRepository networks, IUnitOfWork unitOfWork)
    : IDomainEventHandler<TaskCancelled>
{
    public async Task HandleAsync(TaskCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        var network = await networks.FindByProjectIdAsync(domainEvent.ProjectId, cancellationToken);
        if (network is null)
            return;

        network.RemoveDependenciesOf([domainEvent.TaskId]);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
