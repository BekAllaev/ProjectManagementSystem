using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Funding;

public interface IProjectFundingRepository : IRepository<ProjectFunding>
{
    /// <summary>
    /// There is at most one funding per project (unique ProjectId in the storage).
    /// </summary>
    Task<ProjectFunding?> FindByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
