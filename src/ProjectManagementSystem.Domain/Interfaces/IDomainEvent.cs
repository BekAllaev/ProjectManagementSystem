namespace ProjectManagementSystem.Domain.Interfaces;

/// <summary>
/// Marker for facts that happened inside an aggregate.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Time-ordered id (UUID v7), so events can be sorted and deduplicated by it.
    /// </summary>
    Guid EventId { get; }

    DateTime OccurredAt { get; }
}
