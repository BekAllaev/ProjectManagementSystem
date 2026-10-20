using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Schedule;

public interface IScheduleNetworkRepository : IRepository<ScheduleNetwork>
{
    /// <summary>
    /// There is at most one network per project (unique ProjectId in the storage).
    /// </summary>
    Task<ScheduleNetwork?> FindByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
