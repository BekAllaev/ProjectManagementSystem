namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Finds work packages by something other than their id, because the repository works only by id.
/// Implemented by the infrastructure over the same storage as the aggregates. It only tells which
/// aggregates to load: the rules are always checked again on the loaded aggregates.
/// </summary>
public interface IWorkPackageLookup
{
    Task<Guid?> FindWorkPackageIdOfTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Work packages of the project in which the employee is responsible for the open package or for
    /// one of its unfinished tasks.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListWorkPackageIdsWithOpenWorkOfAsync(
        Guid projectId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
