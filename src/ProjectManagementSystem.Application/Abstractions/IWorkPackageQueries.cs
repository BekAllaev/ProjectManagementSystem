namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Searches work packages by something other than their id. Implemented on the read side, so results
/// may be slightly stale: they only tell which aggregates to load, and the rules are checked again on
/// the loaded aggregates.
/// </summary>
public interface IWorkPackageQueries
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
