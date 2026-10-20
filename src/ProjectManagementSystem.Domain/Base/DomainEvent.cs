using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

/// <summary>
/// Base for all domain events: gives every event its id and the moment it happened.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
