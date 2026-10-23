namespace ProjectManagementSystem.Infrastructure.Persistence.Outbox;

/// <summary>
/// Event type whose messages are not delivered until the record is removed by hand. The rest of the queue goes on.
/// Parked messages are kept, not deleted.
/// </summary>
public sealed class ParkedEventType
{
    public required string EventType { get; init; }

    public DateTime ParkedAt { get; init; }

    public required string Reason { get; init; }
}
