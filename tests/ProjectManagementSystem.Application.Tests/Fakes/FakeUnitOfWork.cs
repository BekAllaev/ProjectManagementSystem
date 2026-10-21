using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// Does what the real unit of work will do with the outbox: on save it takes the events of every aggregate
/// touched through a <see cref="FakeRepository{TAggregate}"/>, clears them and keeps them in
/// <see cref="CollectedEvents"/>.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<IAggregateRoot<Guid>> _tracked = [];
    private readonly List<IDomainEvent> _collectedEvents = [];

    public int SaveCount { get; private set; }

    public IReadOnlyList<IDomainEvent> CollectedEvents => _collectedEvents;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        foreach (var aggregate in _tracked)
        {
            _collectedEvents.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        _tracked.Clear();
        return Task.CompletedTask;
    }

    internal void Track(IAggregateRoot<Guid> aggregate)
    {
        if (!_tracked.Contains(aggregate))
            _tracked.Add(aggregate);
    }
}
