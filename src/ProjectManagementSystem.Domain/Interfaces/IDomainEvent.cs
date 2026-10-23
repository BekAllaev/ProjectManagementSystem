namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// Marker for facts that happened inside an aggregate.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Time-ordered id (UUID v7), so events can be sorted and deduplicated by it.
    /// <see cref="Guid.Empty"/> until the event is saved: the unit of work sets it when it writes the event to the outbox.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Moment the event was saved, in UTC. <see langword="default"/> until the event is saved:
    /// the unit of work sets it from its clock when it writes the event to the outbox.
    /// </summary>
    DateTime OccurredAt { get; }
}
