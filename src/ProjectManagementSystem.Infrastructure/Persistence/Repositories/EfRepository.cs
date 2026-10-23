using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Loads aggregates whole (owned collections come with the root) and registers the written ones in the unit of work,
/// which saves them with their events.
/// </summary>
internal sealed class EfRepository<TAggregate>(AppDbContext dbContext, EfUnitOfWork unitOfWork) : IRepository<TAggregate>
    where TAggregate : AggregateRoot<Guid>
{
    public async Task<TAggregate?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TAggregate>().FindAsync([id], cancellationToken);

    public Task<bool> ExistsAsync(Expression<Func<TAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        dbContext.Set<TAggregate>().AnyAsync(predicate, cancellationToken);

    public void Add(TAggregate aggregate)
    {
        dbContext.Set<TAggregate>().Add(aggregate);
        unitOfWork.Register(aggregate, AggregateChange.Added);
    }

    public void Update(TAggregate aggregate)
    {
        if (dbContext.Entry(aggregate).State == EntityState.Detached)
            dbContext.Set<TAggregate>().Update(aggregate);

        unitOfWork.Register(aggregate, AggregateChange.Updated);
    }

    public void Delete(TAggregate aggregate)
    {
        dbContext.Set<TAggregate>().Remove(aggregate);
        unitOfWork.Register(aggregate, AggregateChange.Deleted);
    }
}
