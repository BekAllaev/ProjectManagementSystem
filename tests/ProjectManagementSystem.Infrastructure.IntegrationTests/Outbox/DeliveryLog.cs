using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Outbox;

/// <summary>
/// Records the events delivered to <see cref="RecordingHandler{TEvent}"/> and lets a test make deliveries of one event
/// type fail.
/// </summary>
public sealed class DeliveryLog
{
    private readonly List<IDomainEvent> _delivered = [];

    public IReadOnlyList<IDomainEvent> Delivered
    {
        get
        {
            lock (_delivered)
                return [.. _delivered];
        }
    }

    public Type? FailingType { get; set; }

    /// <summary>
    /// How many deliveries of <see cref="FailingType"/> still fail; <see langword="null"/> means all of them.
    /// </summary>
    public int? FailuresLeft { get; set; }

    internal void Deliver(IDomainEvent domainEvent)
    {
        if (domainEvent.GetType() == FailingType && FailuresLeft is null or > 0)
        {
            FailuresLeft--;
            throw new InvalidOperationException($"Handler of {FailingType.Name} failed.");
        }

        lock (_delivered)
            _delivered.Add(domainEvent);
    }
}

public sealed class RecordingHandler<TEvent>(DeliveryLog log) : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        log.Deliver(domainEvent);
        return Task.CompletedTask;
    }
}
