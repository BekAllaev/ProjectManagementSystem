using System.Reflection;
using System.Text.Json;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Infrastructure.Outbox;

namespace ProjectManagementSystem.Infrastructure.Tests.Outbox;

public class DomainEventSerializerTests
{
    private static readonly EventTypeRegistry Registry = EventTypeRegistry.FromAssembly(typeof(IDomainEvent).Assembly);
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 9, 30, 15, 123, TimeSpan.Zero);

    private readonly DomainEventSerializer _serializer = new(Registry);

    public static TheoryData<Type> EventTypes => new(Registry.Types.OrderBy(t => t.Name));

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void Every_event_survives_a_round_trip(Type eventType)
    {
        var original = Stamp(CreateSample(eventType));

        var (name, payload) = _serializer.Serialize(original);
        var restored = _serializer.Deserialize(name, payload);

        Assert.Equal(eventType.Name, name);
        Assert.IsType(eventType, restored);
        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.OccurredAt, restored.OccurredAt);
        Assert.Equal(original.AggregateVersion, restored.AggregateVersion);
        Assert.Equal(DateTimeKind.Utc, restored.OccurredAt.Kind);
        foreach (var property in eventType.GetProperties())
            Assert.Equal(Json(property.GetValue(original)), Json(property.GetValue(restored)));
    }

    [Fact]
    public void Nested_record_and_nullable_values_are_kept()
    {
        var dependency = new Dependency(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DependencyType.StartToStart);
        var original = new PredecessorCancelled(Guid.NewGuid(), dependency);

        var restored = Assert.IsType<PredecessorCancelled>(RoundTrip(original));

        Assert.Equal(original, restored);

        var withoutTask = new ResponsiblePersonChanged(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        Assert.Equal(withoutTask, RoundTrip(withoutTask));
    }

    [Fact]
    public void List_dates_and_decimals_are_kept()
    {
        var cancelledTaskIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var original = new WorkPackageCancelled(Guid.NewGuid(), Guid.NewGuid(), "Reason", cancelledTaskIds);

        var restored = Assert.IsType<WorkPackageCancelled>(RoundTrip(original));
        Assert.Equal(cancelledTaskIds, restored.CancelledTaskIds);

        var timeLogged = new ProjectManagementSystem.Domain.TimeTracking.Events.TimeLogged(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 2, 28), 1.75m, 123.45m);
        Assert.Equal(timeLogged, RoundTrip(timeLogged));
    }

    private IDomainEvent RoundTrip(IDomainEvent domainEvent)
    {
        var (name, payload) = _serializer.Serialize(domainEvent);
        return _serializer.Deserialize(name, payload);
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value);

    private static IDomainEvent Stamp(IDomainEvent domainEvent) =>
        ((DomainEvent)domainEvent) with { EventId = Guid.CreateVersion7(Now), OccurredAt = Now.UtcDateTime, AggregateVersion = 3 };

    /// <summary>
    /// An instance of the event with a distinct non-default value in every parameter of its primary constructor.
    /// </summary>
    private static IDomainEvent CreateSample(Type eventType)
    {
        var constructor = eventType.GetConstructors()
            .Where(c => !IsCopyConstructor(c, eventType))
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var counter = 0;
        var arguments = constructor.GetParameters().Select(p => SampleValue(p.ParameterType, ++counter)).ToArray();

        return (IDomainEvent)constructor.Invoke(arguments);
    }

    private static bool IsCopyConstructor(ConstructorInfo constructor, Type type) =>
        constructor.GetParameters() is [var single] && single.ParameterType == type;

    private static object SampleValue(Type type, int seed)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(Guid))
            return new Guid(seed, 0, 0, new byte[8]);
        if (underlying == typeof(string))
            return $"text {seed}";
        if (underlying == typeof(decimal))
            return seed + 0.25m;
        if (underlying == typeof(DateOnly))
            return new DateOnly(2026, 1, 1).AddDays(seed);
        if (underlying == typeof(DateTime))
            return new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddHours(seed);
        if (underlying.IsEnum)
            return Enum.GetValues(underlying).Cast<object>().Last();
        if (underlying == typeof(IReadOnlyList<Guid>))
            return new List<Guid> { new(seed, 1, 0, new byte[8]), new(seed, 2, 0, new byte[8]) };
        if (underlying == typeof(Dependency))
            return new Dependency(
                new Guid(seed, 1, 0, new byte[8]), new Guid(seed, 2, 0, new byte[8]),
                new Guid(seed, 3, 0, new byte[8]), new Guid(seed, 4, 0, new byte[8]),
                DependencyType.FinishToFinish);

        throw new NotSupportedException($"Add a sample value for {type} to the test.");
    }
}
