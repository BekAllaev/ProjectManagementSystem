using System.Linq.Expressions;
using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// CRUD access to aggregates by id; only aggregate roots can be stored. The only search is
/// <see cref="ExistsAsync"/>; loading aggregates by anything else is a query of the application layer. Changes are saved by a unit of work of the application layer,
/// one aggregate per transaction.
/// </summary>
public interface IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// For a rule that depends on the whole set of aggregates (the unique email of an employee). The
    /// predicate is translated by the storage, so it must not call domain code: compute values first and
    /// compare with them.
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TAggregate, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Delete(TAggregate aggregate);
}
