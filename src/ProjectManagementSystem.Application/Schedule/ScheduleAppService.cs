using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Schedule;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule;

public sealed class ScheduleAppService(
    IRepository<ScheduleNetwork> networks,
    IRepository<WorkPackage> workPackages,
    DependencyService dependencyService,
    IUnitOfWork unitOfWork) : IScheduleAppService
{
    /// <summary>
    /// The network of the project is created on its first dependency. The domain service checks that each
    /// task belongs to the given work package.
    /// </summary>
    public async Task AddDependencyAsync(
        Guid projectId,
        Guid predecessorWorkPackageId,
        Guid predecessorTaskId,
        Guid successorWorkPackageId,
        Guid successorTaskId,
        DependencyTypeDto type,
        CancellationToken cancellationToken = default)
    {
        var predecessorWorkPackage = await workPackages.GetAsync(predecessorWorkPackageId, cancellationToken);
        var successorWorkPackage = await workPackages.GetAsync(successorWorkPackageId, cancellationToken);

        var network = await networks.FindAsync(projectId, cancellationToken);
        var isNew = network is null;
        network ??= new ScheduleNetwork(projectId);

        dependencyService.AddDependency(
            network, predecessorWorkPackage, predecessorTaskId, successorWorkPackage, successorTaskId, ToDomain(type));

        if (isNew)
            networks.Add(network);
        else
            networks.Update(network);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDependencyAsync(
        Guid projectId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        CancellationToken cancellationToken = default)
    {
        var network = await networks.GetAsync(projectId, cancellationToken);

        network.RemoveDependency(predecessorTaskId, successorTaskId);

        networks.Update(network);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DependencyType ToDomain(DependencyTypeDto type) => type switch
    {
        DependencyTypeDto.FinishToStart => DependencyType.FinishToStart,
        DependencyTypeDto.StartToStart => DependencyType.StartToStart,
        DependencyTypeDto.FinishToFinish => DependencyType.FinishToFinish,
        DependencyTypeDto.StartToFinish => DependencyType.StartToFinish,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
