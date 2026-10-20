using ProjectManagementSystem.Application.Contracts.Events;

namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Puts an application event into the outbox; it is sent when the unit of work is saved.
/// </summary>
public interface IApplicationEventPublisher
{
    void Publish(ApplicationEvent applicationEvent);
}
