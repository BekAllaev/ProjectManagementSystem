namespace ProjectManagementSystem.Application.Contracts.TimeTracking;

public interface ITimeTrackingAppService
{
    /// <returns>Id of the new time entry.</returns>
    Task<Guid> LogTimeAsync(
        Guid workPackageId,
        Guid taskId,
        Guid employeeId,
        DateOnly workDate,
        decimal hours,
        CancellationToken cancellationToken = default);

    Task RemoveTimeEntryAsync(Guid taskId, Guid entryId, CancellationToken cancellationToken = default);
}
