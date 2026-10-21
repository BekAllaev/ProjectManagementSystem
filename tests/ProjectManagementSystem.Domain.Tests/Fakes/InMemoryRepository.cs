using System.Linq.Expressions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Tests.Fakes;

public sealed class InMemoryRepository<TAggregate> : IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    private readonly Dictionary<Guid, TAggregate> _items = [];

    public IReadOnlyCollection<TAggregate> All => _items.Values;

    public Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<bool> ExistsAsync(Expression<Func<TAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Any(predicate.Compile()));

    public void Add(TAggregate aggregate) => _items.Add(aggregate.Id, aggregate);

    public void Update(TAggregate aggregate)
    {
        if (!_items.ContainsKey(aggregate.Id))
            throw new InvalidOperationException($"{typeof(TAggregate).Name} '{aggregate.Id}' was never added.");
    }

    public void Delete(TAggregate aggregate) => _items.Remove(aggregate.Id);
}
