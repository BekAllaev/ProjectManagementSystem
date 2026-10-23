namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Saves changed aggregates together with their events (into the outbox) in one transaction.
/// Fails with <see cref="Exceptions.ConcurrencyConflictException"/> if an aggregate was changed by someone else
/// since it was loaded (optimistic concurrency).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
