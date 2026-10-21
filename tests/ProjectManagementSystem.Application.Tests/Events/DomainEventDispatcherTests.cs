using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Tests.Events;

public class DomainEventDispatcherTests
{
    private readonly List<string> _calls = [];

    [Fact]
    public async Task Dispatch_EventAsInterface_CallsEveryHandlerOfItsRuntimeType()
    {
        using var app = new TestApplication(services =>
        {
            services.AddSingleton(_calls);
            services.AddScoped<IDomainEventHandler<SampleEvent>, FirstSampleHandler>();
            services.AddScoped<IDomainEventHandler<SampleEvent>, SecondSampleHandler>();
            services.AddScoped<IDomainEventHandler<OtherEvent>, OtherEventHandler>();
        });
        IDomainEvent domainEvent = new SampleEvent("payload");

        await app.DispatchAsync(domainEvent);

        Assert.Equal(["first:payload", "second:payload"], _calls);
    }

    [Fact]
    public async Task Dispatch_EventWithoutHandlers_DoesNothing()
    {
        using var app = new TestApplication(services => services.AddSingleton(_calls));

        await app.DispatchAsync(new SampleEvent("payload"));

        Assert.Empty(_calls);
    }

    private sealed record SampleEvent(string Payload) : DomainEvent;

    private sealed record OtherEvent : DomainEvent;

    private sealed class FirstSampleHandler(List<string> calls) : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken = default)
        {
            calls.Add($"first:{domainEvent.Payload}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondSampleHandler(List<string> calls) : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken = default)
        {
            calls.Add($"second:{domainEvent.Payload}");
            return Task.CompletedTask;
        }
    }

    private sealed class OtherEventHandler(List<string> calls) : IDomainEventHandler<OtherEvent>
    {
        public Task HandleAsync(OtherEvent domainEvent, CancellationToken cancellationToken = default)
        {
            calls.Add("other");
            return Task.CompletedTask;
        }
    }
}
