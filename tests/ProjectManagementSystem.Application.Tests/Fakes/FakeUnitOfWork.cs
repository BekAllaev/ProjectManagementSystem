using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// Does what the real unit of work will do with the outbox: on save it takes the events of every aggregate
/// touched through a <see cref="FakeRepository{TAggregate}"/>, clears them and keeps them in
/// <see cref="CollectedEvents"/>. It also records which aggregates the saved command changed.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<IAggregateRoot<Guid>> _tracked = [];
    private readonly List<IAggregateRoot<Guid>> _written = [];
    private readonly List<IDomainEvent> _collectedEvents = [];

    public int SaveCount { get; private set; }

    public IReadOnlyList<IDomainEvent> CollectedEvents => _collectedEvents;

    /// <summary>
    /// Aggregates changed by the last save: added, updated or deleted through a repository, or with new events.
    /// Aggregates that were only loaded are not here.
    /// </summary>
    public IReadOnlyList<IAggregateRoot<Guid>> ChangedAggregates { get; private set; } = [];

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        ChangedAggregates = _tracked.Where(a => _written.Contains(a) || a.DomainEvents.Count > 0).ToList();

        foreach (var aggregate in _tracked)
        {
            _collectedEvents.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        _tracked.Clear();
        _written.Clear();
        return Task.CompletedTask;
    }

    internal void Track(IAggregateRoot<Guid> aggregate, bool written = false)
    {
        if (!_tracked.Contains(aggregate))
            _tracked.Add(aggregate);

        if (written && !_written.Contains(aggregate))
            _written.Add(aggregate);
    }
}
