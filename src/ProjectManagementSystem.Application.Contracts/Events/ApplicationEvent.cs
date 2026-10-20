namespace ProjectManagementSystem.Application.Contracts.Events;

/// <summary>
/// Fact noticed by the application layer, not raised by an aggregate. Usually a signal for a person
/// to react manually.
/// </summary>
public abstract record ApplicationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
