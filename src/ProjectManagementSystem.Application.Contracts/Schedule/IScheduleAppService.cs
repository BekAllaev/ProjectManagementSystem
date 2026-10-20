namespace ProjectManagementSystem.Application.Contracts.Schedule;

public interface IScheduleAppService
{
    Task AddDependencyAsync(
        Guid projectId,
        Guid predecessorWorkPackageId,
        Guid predecessorTaskId,
        Guid successorWorkPackageId,
        Guid successorTaskId,
        DependencyTypeDto type,
        CancellationToken cancellationToken = default);

    Task RemoveDependencyAsync(
        Guid projectId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        CancellationToken cancellationToken = default);
}
