using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// CRUD access to aggregates by id; only aggregate roots can be stored. Searching by anything else is
/// a query of the application layer. Changes are saved by a unit of work of the application layer,
/// one aggregate per transaction. The only exception is creating a work package, which also registers it
/// in its project (see <see cref="Services.WorkAssignmentService.CreateWorkPackage"/>).
/// </summary>
public interface IRepository<TAggregate> where TAggregate : AggregateRoot<Guid>
{
    Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Delete(TAggregate aggregate);
}
