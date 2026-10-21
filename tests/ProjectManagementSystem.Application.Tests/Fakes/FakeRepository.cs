using System.Linq.Expressions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// In-memory repository that reports every loaded, added, updated or deleted aggregate to the
/// <see cref="FakeUnitOfWork"/>, like change tracking of the real persistence.
/// </summary>
public sealed class FakeRepository<TAggregate>(FakeUnitOfWork unitOfWork) : IRepository<TAggregate>
    where TAggregate : AggregateRoot<Guid>
{
    private readonly Dictionary<Guid, TAggregate> _items = [];

    public IReadOnlyCollection<TAggregate> All => _items.Values;

    /// <summary>
    /// Stores an aggregate arranged with domain methods as if it had been saved earlier: its events are dropped.
    /// </summary>
    public TAggregate Seed(TAggregate aggregate)
    {
        aggregate.ClearDomainEvents();
        _items[aggregate.Id] = aggregate;

        return aggregate;
    }

    public Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = _items.GetValueOrDefault(id);
        if (aggregate is not null)
            unitOfWork.Track(aggregate);

        return Task.FromResult(aggregate);
    }

    public Task<bool> ExistsAsync(Expression<Func<TAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Any(predicate.Compile()));

    public void Add(TAggregate aggregate)
    {
        _items.Add(aggregate.Id, aggregate);
        unitOfWork.Track(aggregate, written: true);
    }

    public void Update(TAggregate aggregate)
    {
        if (!_items.ContainsKey(aggregate.Id))
            throw new InvalidOperationException($"{typeof(TAggregate).Name} '{aggregate.Id}' was never added.");

        unitOfWork.Track(aggregate, written: true);
    }

    public void Delete(TAggregate aggregate)
    {
        _items.Remove(aggregate.Id);
        unitOfWork.Track(aggregate, written: true);
    }
}
