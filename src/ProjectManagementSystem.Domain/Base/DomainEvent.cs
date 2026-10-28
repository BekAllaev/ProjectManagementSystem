using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

/// <summary>
/// Base for all domain events. <see cref="EventId"/>, <see cref="OccurredAt"/> and <see cref="AggregateVersion"/> are
/// empty while the event is only raised: the unit of work sets them when it writes the event to the outbox.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }

    public DateTime OccurredAt { get; init; }

    public long AggregateVersion { get; init; }
}
