using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Infrastructure.Outbox;
using ProjectManagementSystem.Infrastructure.Persistence.Configurations;
using ProjectManagementSystem.Infrastructure.Persistence.Outbox;

namespace ProjectManagementSystem.Infrastructure.Persistence;

internal enum AggregateChange
{
    Added,
    Updated,
    Deleted,
}

/// <summary>
/// Saves the aggregates registered by the repositories of the current scope together with their events, written to
/// the outbox in the same <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> (one implicit transaction, so it
/// works with the retrying execution strategy). Wakes the outbox processor after the commit.
/// </summary>
internal sealed class EfUnitOfWork(
    AppDbContext dbContext,
    DomainEventSerializer serializer,
    OutboxSignal outboxSignal,
    TimeProvider timeProvider) : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly List<(AggregateRoot<Guid> Aggregate, AggregateChange Change)> _registered = [];

    public void Register(AggregateRoot<Guid> aggregate, AggregateChange change)
    {
        var index = _registered.FindIndex(r => ReferenceEquals(r.Aggregate, aggregate));
        if (index < 0)
            _registered.Add((aggregate, change));
        else if (change == AggregateChange.Deleted || _registered[index].Change == AggregateChange.Updated)
            _registered[index] = (aggregate, change);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureEveryChangedAggregateIsRegistered();

        foreach (var (aggregate, _) in _registered.Where(r => r.Change == AggregateChange.Updated))
            IncrementVersion(aggregate);

        var outboxMessageCount = WriteEventsToOutbox();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var aggregate = exception.Entries.Select(e => e.Entity).OfType<AggregateRoot<Guid>>().FirstOrDefault();
            throw new ConcurrencyConflictException(aggregate?.GetType().Name ?? "Aggregate", aggregate?.Id ?? Guid.Empty, exception);
        }
        catch (DbUpdateException exception) when (IsTakenEmail(exception))
        {
            var employee = exception.Entries.Select(e => e.Entity).OfType<Employee>().FirstOrDefault()
                ?? _registered.Select(r => r.Aggregate).OfType<Employee>().First();
            throw EmployeeService.EmailIsTaken(employee.NormalizedEmail);
        }

        _registered.Clear();

        if (outboxMessageCount > 0)
            outboxSignal.Notify();
    }

    /// <summary>
    /// An aggregate that raised events but was not passed to Add, Update or Delete would lose its events and
    /// would be saved without a new version, so it is a bug of the caller.
    /// </summary>
    private void EnsureEveryChangedAggregateIsRegistered()
    {
        var unregistered = dbContext.ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .FirstOrDefault(a => a.DomainEvents.Count > 0 && !_registered.Exists(r => ReferenceEquals(r.Aggregate, a)));

        if (unregistered is not null)
            throw new InvalidOperationException(
                $"{unregistered.GetType().Name} '{unregistered.Id}' raised events but was not passed to the repository " +
                "(Add, Update or Delete).");
    }

    /// <summary>
    /// The version is the concurrency token: the update is checked against the loaded version. It is raised for every
    /// updated aggregate, because a change of an owned collection alone does not mark the root as modified.
    /// </summary>
    private void IncrementVersion(AggregateRoot<Guid> aggregate)
    {
        var version = dbContext.Entry(aggregate).Property(a => a.Version);
        version.CurrentValue = version.OriginalValue + 1;
    }

    private int WriteEventsToOutbox()
    {
        var now = timeProvider.GetUtcNow();
        var count = 0;

        foreach (var (aggregate, _) in _registered)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var stamped = Stamp(domainEvent, now, aggregate.Version);
                var (eventType, payload) = serializer.Serialize(stamped);

                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    EventId = stamped.EventId,
                    EventType = eventType,
                    Payload = payload,
                    OccurredAt = stamped.OccurredAt,
                });
                count++;
            }

            aggregate.ClearDomainEvents();
        }

        return count;
    }

    /// <param name="version">Version the aggregate is saved with: already raised for an updated aggregate.</param>
    private static IDomainEvent Stamp(IDomainEvent domainEvent, DateTimeOffset now, long version) =>
        domainEvent is DomainEvent stampable
            ? stampable with { EventId = Guid.CreateVersion7(now), OccurredAt = now.UtcDateTime, AggregateVersion = version }
            : throw new InvalidOperationException(
                $"Domain event '{domainEvent.GetType().Name}' must derive from {nameof(DomainEvent)} to be saved.");

    private static bool IsTakenEmail(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation } sqlException &&
        sqlException.Message.Contains(EmployeeConfiguration.NormalizedEmailIndex, StringComparison.Ordinal);
}
