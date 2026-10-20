using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Events;

namespace ProjectManagementSystem.Application.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class FakeApplicationEventPublisher : IApplicationEventPublisher
{
    public List<ApplicationEvent> Published { get; } = [];

    public void Publish(ApplicationEvent applicationEvent) => Published.Add(applicationEvent);
}
