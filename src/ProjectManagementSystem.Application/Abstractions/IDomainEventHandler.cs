using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Reacts to a domain event after the transaction that raised it is committed. Events come from the
/// transactional outbox one by one in the order they were raised, and may be delivered more than once,
/// so every handler must be idempotent.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
