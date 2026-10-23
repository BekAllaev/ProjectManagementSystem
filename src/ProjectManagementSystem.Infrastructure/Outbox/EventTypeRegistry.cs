using System.Reflection;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Explicit map between the stable name of an event type, stored in the outbox, and the CLR type. The name is the name
/// of the class, so moving an event to another namespace or assembly does not break stored messages; renaming the
/// class does.
/// </summary>
public sealed class EventTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByName;
    private readonly Dictionary<Type, string> _namesByType;

    /// <exception cref="InvalidOperationException">Two event types have the same name.</exception>
    public EventTypeRegistry(IEnumerable<Type> eventTypes)
    {
        var types = eventTypes.ToList();

        var duplicates = types.GroupBy(t => t.Name).Where(g => g.Count() > 1).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                "Domain event types must have unique names: " +
                string.Join("; ", duplicates.Select(g => string.Join(", ", g.Select(t => t.FullName)))));

        _typesByName = types.ToDictionary(t => t.Name);
        _namesByType = types.ToDictionary(t => t, t => t.Name);
    }

    public IReadOnlyCollection<Type> Types => _namesByType.Keys;

    /// <summary>
    /// Every concrete <see cref="IDomainEvent"/> of the assembly.
    /// </summary>
    public static EventTypeRegistry FromAssembly(Assembly assembly) =>
        new(assembly.GetTypes().Where(t =>
            t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
            typeof(IDomainEvent).IsAssignableFrom(t)));

    /// <exception cref="InvalidOperationException">The type is not registered.</exception>
    public string GetName(Type eventType) =>
        _namesByType.GetValueOrDefault(eventType)
        ?? throw new InvalidOperationException($"Domain event type '{eventType.FullName}' is not registered.");

    /// <exception cref="InvalidOperationException">No type has this name.</exception>
    public Type GetType(string name) =>
        _typesByName.GetValueOrDefault(name)
        ?? throw new InvalidOperationException($"Unknown domain event type '{name}'.");
}
