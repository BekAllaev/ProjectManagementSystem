using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.WorkPackages;

public interface IWorkPackageRepository : IRepository<WorkPackage>
{
    Task<WorkPackage?> FindByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkPackage>> ListByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
