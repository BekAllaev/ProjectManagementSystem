using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// The real <c>AddApplication()</c> with fakes for everything the infrastructure will provide. Every
/// request and every dispatch runs in its own scope, like a web request or one outbox message.
/// </summary>
public sealed class TestApplication : IDisposable
{
    private readonly ServiceProvider _services;
    private int _deliveredEventCount;

    public TestApplication(Action<IServiceCollection>? configure = null)
    {
        UnitOfWork = new(TimeProvider);
        Projects = new(UnitOfWork);
        WorkPackages = new(UnitOfWork);
        Networks = new(UnitOfWork);
        TimeLogs = new(UnitOfWork);
        Milestones = new(UnitOfWork);
        Fundings = new(UnitOfWork);
        Risks = new(UnitOfWork);
        Employees = new(UnitOfWork);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider);
        services.AddApplication();
        services.AddSingleton<IUnitOfWork>(UnitOfWork);
        services.AddSingleton<INotificationSender>(Notifications);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IRepository<Project>>(Projects);
        services.AddSingleton<IRepository<WorkPackage>>(WorkPackages);
        services.AddSingleton<IRepository<ScheduleNetwork>>(Networks);
        services.AddSingleton<IRepository<TaskTimeLog>>(TimeLogs);
        services.AddSingleton<IRepository<Milestone>>(Milestones);
        services.AddSingleton<IRepository<ProjectFunding>>(Fundings);
        services.AddSingleton<IRepository<Risk>>(Risks);
        services.AddSingleton<IRepository<Employee>>(Employees);
        services.AddSingleton(typeof(IReadModelRepository<>), typeof(FakeReadModelRepository<>));
        configure?.Invoke(services);

        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>
    /// Start of <see cref="TestData.Today"/>, the day the team members of <see cref="TestData"/> are counted from.
    /// </summary>
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(TestData.Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    public FakeUnitOfWork UnitOfWork { get; }

    public FakeNotificationSender Notifications { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeRepository<Project> Projects { get; }

    public FakeRepository<WorkPackage> WorkPackages { get; }

    public FakeRepository<ScheduleNetwork> Networks { get; }

    public FakeRepository<TaskTimeLog> TimeLogs { get; }

    public FakeRepository<Milestone> Milestones { get; }

    public FakeRepository<ProjectFunding> Fundings { get; }

    public FakeRepository<Risk> Risks { get; }

    public FakeRepository<Employee> Employees { get; }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        await using var scope = _services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(domainEvent);
    }

    /// <summary>
    /// Stands in for the outbox processor: dispatches every collected event that was not delivered yet, in the
    /// order of saving, including the events saved by the handlers themselves.
    /// </summary>
    /// <returns>The delivered events, for a redelivery.</returns>
    public async Task<IReadOnlyList<IDomainEvent>> DeliverEventsAsync()
    {
        var delivered = new List<IDomainEvent>();

        while (_deliveredEventCount < UnitOfWork.CollectedEvents.Count)
        {
            var domainEvent = UnitOfWork.CollectedEvents[_deliveredEventCount++];
            await DispatchAsync(domainEvent);
            delivered.Add(domainEvent);
        }

        return delivered;
    }

    /// <summary>
    /// Delivers the given events once more, as the outbox does after a crash before marking them processed.
    /// </summary>
    public async Task RedeliverAsync(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events.ToList())
            await DispatchAsync(domainEvent);
    }

    public void Dispose() => _services.Dispose();
}
