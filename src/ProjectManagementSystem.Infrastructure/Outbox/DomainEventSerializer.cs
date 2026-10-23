using System.Text.Json;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Turns a domain event into the stable name of its type and a JSON payload for the outbox, and back.
/// </summary>
public sealed class DomainEventSerializer(EventTypeRegistry registry)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    public (string EventType, string Payload) Serialize(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();

        return (registry.GetName(type), JsonSerializer.Serialize(domainEvent, type, Options));
    }

    public IDomainEvent Deserialize(string eventType, string payload)
    {
        var type = registry.GetType(eventType);

        return (IDomainEvent)(JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException($"Payload of domain event '{eventType}' is empty."));
    }
}
