namespace ProjectManagementSystem.Domain.Interfaces;

public interface IAggregateRoot<TKey> : IEntity<TKey>
{
    /// <summary>
    /// Version of the aggregate for optimistic concurrency.
    /// </summary>
    long Version { get; }

    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
