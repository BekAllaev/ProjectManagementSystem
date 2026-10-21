using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Events;

/// <summary>
/// Resolves the handlers by the runtime type of the event, because the outbox gives events as
/// <see cref="IDomainEvent"/>. Handlers run one by one in the order of registration.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly MethodInfo DispatchToHandlersMethod =
        typeof(DomainEventDispatcher).GetMethod(nameof(DispatchToHandlersAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var dispatch = DispatchToHandlersMethod.MakeGenericMethod(domainEvent.GetType());

        return (Task)dispatch.Invoke(this, [domainEvent, cancellationToken])!;
    }

    private async Task DispatchToHandlersAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
            await handler.HandleAsync(domainEvent, cancellationToken);
    }
}
