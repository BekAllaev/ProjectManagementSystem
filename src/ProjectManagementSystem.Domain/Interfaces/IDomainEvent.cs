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

    /// <summary>
    /// Version of the aggregate that raised the event, as saved together with it; all events of one save have the same
    /// version. 0 until the event is saved: the unit of work sets it when it writes the event to the outbox. The read
    /// side keeps it, so a read model tells which version of the aggregate it shows.
    /// </summary>
    long AggregateVersion { get; }
}
