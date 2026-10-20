using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Events;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Application.Tests.Fakes;

public sealed class InMemoryRepository<TAggregate> : IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    private readonly Dictionary<Guid, TAggregate> _items = [];

    public IReadOnlyCollection<TAggregate> All => _items.Values;

    public Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public void Add(TAggregate aggregate) => _items.Add(aggregate.Id, aggregate);

    public void Update(TAggregate aggregate)
    {
        if (!_items.ContainsKey(aggregate.Id))
            throw new InvalidOperationException($"{typeof(TAggregate).Name} '{aggregate.Id}' was never added.");
    }

    public void Delete(TAggregate aggregate) => _items.Remove(aggregate.Id);
}

public sealed class InMemoryWorkPackageLookup(InMemoryRepository<WorkPackage> workPackages) : IWorkPackageLookup
{
    public Task<Guid?> FindWorkPackageIdOfTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult(workPackages.All.SingleOrDefault(wp => wp.ContainsTask(taskId))?.Id);

    public Task<IReadOnlyList<Guid>> ListWorkPackageIdsWithOpenWorkOfAsync(
        Guid projectId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var isResponsible = new IsResponsibleForOpenWorkSpecification(employeeId);

        return Task.FromResult<IReadOnlyList<Guid>>(workPackages.All
            .Where(wp => wp.ProjectId == projectId && isResponsible.IsSatisfiedBy(wp))
            .Select(wp => wp.Id)
            .ToList());
    }
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
