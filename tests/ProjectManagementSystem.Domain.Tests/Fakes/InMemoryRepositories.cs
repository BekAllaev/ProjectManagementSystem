using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Tests.Fakes;

public class InMemoryRepository<TAggregate> : IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    protected readonly List<TAggregate> Items = [];

    public Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(a => a.Id == id));

    public void Add(TAggregate aggregate) => Items.Add(aggregate);
}

public sealed class InMemoryWorkPackageRepository : InMemoryRepository<WorkPackage>, IWorkPackageRepository
{
    public Task<WorkPackage?> FindByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(wp => wp.ContainsTask(taskId)));

    public Task<IReadOnlyList<WorkPackage>> ListByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkPackage>>(Items.Where(wp => wp.ProjectId == projectId).ToList());
}

public sealed class InMemoryScheduleNetworkRepository : InMemoryRepository<ScheduleNetwork>, IScheduleNetworkRepository
{
    public Task<ScheduleNetwork?> FindByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(n => n.ProjectId == projectId));
}

public sealed class InMemoryTaskTimeLogRepository : InMemoryRepository<TaskTimeLog>, ITaskTimeLogRepository
{
}
