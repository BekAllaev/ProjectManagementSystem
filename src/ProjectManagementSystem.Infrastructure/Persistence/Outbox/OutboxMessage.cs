namespace ProjectManagementSystem.Infrastructure.Persistence.Outbox;

/// <summary>
/// Domain event saved in the same transaction as the aggregate that raised it. Messages are delivered one by one
/// in the order of <see cref="Sequence"/>.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Global order of delivery, given by the database on insert.
    /// </summary>
    public long Sequence { get; private set; }

    public Guid EventId { get; init; }

    /// <summary>
    /// Stable name of the event type, not the CLR name with the assembly.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The event serialized to JSON.
    /// </summary>
    public required string Payload { get; init; }

    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Empty until every handler of the event has run.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Failed deliveries so far.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// After a failed delivery: the queue waits for this message until this moment.
    /// </summary>
    public DateTime? NextAttemptAt { get; set; }

    public string? LastError { get; set; }

    /// <summary>
    /// Set when the type of the event was parked after this message failed too often, see <see cref="ParkedEventType"/>.
    /// </summary>
    public DateTime? ParkedAt { get; set; }
}
