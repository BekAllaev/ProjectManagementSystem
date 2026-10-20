using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

public class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Set by the persistence layer and used as a concurrency token: a save fails if the stored
    /// version differs from the one the aggregate was loaded with. The domain never changes it.
    /// </summary>
    public long Version { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public AggregateRoot(TKey id) : base(id)
    {
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
