namespace ProjectManagementSystem.Domain.Interfaces;

public interface IAggregateRoot<TKey> : IEntity<TKey>
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
