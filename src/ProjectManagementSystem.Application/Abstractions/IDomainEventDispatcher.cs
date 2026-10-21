using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Passes a committed domain event to every <see cref="IDomainEventHandler{TEvent}"/> of its runtime type.
/// Called by the outbox processor of the infrastructure; an event without handlers is ignored.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
