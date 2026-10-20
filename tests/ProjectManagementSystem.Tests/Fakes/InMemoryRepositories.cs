using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Events;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Tests.Fakes;

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

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class FakeApplicationEventPublisher : IApplicationEventPublisher
{
    public List<ApplicationEvent> Published { get; } = [];

    public void Publish(ApplicationEvent applicationEvent) => Published.Add(applicationEvent);
}
