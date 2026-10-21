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

    public TestApplication(Action<IServiceCollection>? configure = null)
    {
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
        services.AddSingleton<IRepository<Project>>(Projects);
        services.AddSingleton<IRepository<WorkPackage>>(WorkPackages);
        services.AddSingleton<IRepository<ScheduleNetwork>>(Networks);
        services.AddSingleton<IRepository<TaskTimeLog>>(TimeLogs);
        services.AddSingleton<IRepository<Milestone>>(Milestones);
        services.AddSingleton<IRepository<ProjectFunding>>(Fundings);
        services.AddSingleton<IRepository<Risk>>(Risks);
        services.AddSingleton<IRepository<Employee>>(Employees);
        configure?.Invoke(services);

        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>
    /// Start of <see cref="TestData.Today"/>, the day the team members of <see cref="TestData"/> are counted from.
    /// </summary>
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(TestData.Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    public FakeUnitOfWork UnitOfWork { get; } = new();

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

    public void Dispose() => _services.Dispose();
}
