using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Schedule;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Schedule;

public sealed class ScheduleAppService(
    IRepository<ScheduleNetwork> networks,
    IRepository<WorkPackage> workPackages,
    IWorkPackageLookup workPackageLookup,
    DependencyService dependencyService,
    IUnitOfWork unitOfWork) : IScheduleAppService
{
    /// <summary>
    /// The network of the project is created on its first dependency.
    /// </summary>
    public async Task AddDependencyAsync(
        Guid projectId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        DependencyTypeDto type,
        CancellationToken cancellationToken = default)
    {
        var predecessorWorkPackage = await GetWorkPackageOfTaskAsync(predecessorTaskId, cancellationToken);
        var successorWorkPackage = await GetWorkPackageOfTaskAsync(successorTaskId, cancellationToken);

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

    private async Task<WorkPackage> GetWorkPackageOfTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var workPackageId = await workPackageLookup.FindWorkPackageIdOfTaskAsync(taskId, cancellationToken)
            ?? throw new EntityNotFoundException("Task", taskId);

        return await workPackages.GetAsync(workPackageId, cancellationToken);
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
