using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

/// <summary>
/// Base for all domain events. <see cref="EventId"/> and <see cref="OccurredAt"/> are empty while the event
/// is only raised: the unit of work sets them from its clock when it writes the event to the outbox.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }

    public DateTime OccurredAt { get; init; }
}
