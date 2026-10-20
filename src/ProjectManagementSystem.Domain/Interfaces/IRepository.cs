using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// CRUD access to aggregates by id; only aggregate roots can be stored. Searching by anything else is
/// a query of the application layer. Changes are saved by a unit of work of the application layer,
/// one aggregate per transaction.
/// </summary>
public interface IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Delete(TAggregate aggregate);
}
