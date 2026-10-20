using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;

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

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
