using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Infrastructure.Outbox;

namespace ProjectManagementSystem.Infrastructure.Tests.Outbox;

public class EventTypeRegistryTests
{
    private static readonly EventTypeRegistry Registry = EventTypeRegistry.FromAssembly(typeof(IDomainEvent).Assembly);

    [Fact]
    public void Registers_every_domain_event_of_the_domain_under_its_class_name()
    {
        var eventTypes = typeof(IDomainEvent).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(eventTypes);
        Assert.Equal(eventTypes.ToHashSet(), Registry.Types.ToHashSet());
        Assert.All(eventTypes, t => Assert.Equal(t, Registry.GetType(Registry.GetName(t))));
        Assert.Equal("PredecessorCancelled", Registry.GetName(typeof(PredecessorCancelled)));
    }

    [Fact]
    public void Rejects_two_event_types_with_the_same_name()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new EventTypeRegistry([typeof(First.SameName), typeof(Second.SameName)]));

        Assert.Contains(typeof(First.SameName).FullName!, exception.Message);
        Assert.Contains(typeof(Second.SameName).FullName!, exception.Message);
    }

    [Fact]
    public void Unknown_name_and_unregistered_type_are_errors()
    {
        Assert.Throws<InvalidOperationException>(() => Registry.GetType("NoSuchEvent"));
        Assert.Throws<InvalidOperationException>(() => Registry.GetName(typeof(First.SameName)));
    }

    private static class First
    {
        public sealed record SameName : DomainEvent;
    }

    private static class Second
    {
        public sealed record SameName : DomainEvent;
    }
}
