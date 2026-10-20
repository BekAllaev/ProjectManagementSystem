using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// Loads and stores one kind of aggregate. Changes are saved by a unit of work of the application layer,
/// one aggregate per transaction.
/// </summary>
public interface IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);
}
