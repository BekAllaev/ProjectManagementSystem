using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.Schedule;

public class ScheduleCommandTests : CommandTestBase
{
    private readonly Project _project;
    private readonly WorkPackage _first;
    private readonly WorkPackage _second;

    public ScheduleCommandTests()
    {
        _project = SeedProject();
        _first = SeedWorkPackage(_project, taskCount: 2);
        _second = SeedWorkPackage(_project, taskCount: 1);
    }

    private Guid A => _first.Tasks.First().Id;

    private Guid B => _first.Tasks.Last().Id;

    private Guid C => _second.Tasks.Single().Id;

    [Fact]
    public async Task AddDependency_FirstDependency_CreatesNetworkOfProject()
    {
        await App.SendAsync(Dependency(_first, A, _second, C));

        var network = Assert.Single(App.Networks.All);
        Assert.Equal(_project.Id, network.ProjectId);
        Assert.Equal(new Dependency(A, _first.Id, C, _second.Id, DependencyType.FinishToStart), network.Dependencies.Single());
        AssertCommitted(network, typeof(DependencyAdded));
    }

    [Fact]
    public async Task AddDependency_ExistingNetwork_AddsToIt()
    {
        var network = SeedNetworkWith(A, B);

        await App.SendAsync(Dependency(_first, B, _second, C));

        Assert.Equal(2, network.Dependencies.Count);
        AssertCommitted(network, typeof(DependencyAdded));
    }

    [Fact]
    public async Task AddDependency_Cycle_ThrowsDomainException()
    {
        var network = SeedNetworkWith(A, B);

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(Dependency(_first, B, _first, A)));
        Assert.Single(network.Dependencies);
    }

    [Fact]
    public async Task AddDependency_CancelledTask_ThrowsDomainException()
    {
        _second.CancelTask(C);
        _second.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(Dependency(_first, A, _second, C)));
        Assert.Empty(App.Networks.All);
    }

    [Fact]
    public async Task AddDependency_ClosedProject_ThrowsDomainException()
    {
        TestData.Close(_project);
        _project.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(Dependency(_first, A, _second, C)));
    }

    [Fact]
    public async Task AddDependency_TaskOfAnotherWorkPackage_ThrowsEntityNotFoundException()
    {
        await AssertRejectedAsync<EntityNotFoundException>(() => App.SendAsync(Dependency(_second, A, _second, C)));
    }

    [Fact]
    public async Task RemoveDependency_ExistingDependency_RemovesIt()
    {
        var network = SeedNetworkWith(A, B);

        await App.SendAsync(new RemoveDependencyCommand(_project.Id, A, B));

        Assert.Empty(network.Dependencies);
        AssertCommitted(network, typeof(DependencyRemoved));
    }

    [Fact]
    public async Task RemoveDependency_MissingDependency_ThrowsDomainException()
    {
        SeedNetworkWith(A, B);

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(new RemoveDependencyCommand(_project.Id, B, A)));
    }

    [Fact]
    public async Task RemoveDependency_ProjectWithoutNetwork_ThrowsNotFoundException()
    {
        await AssertRejectedAsync<NotFoundException>(() => App.SendAsync(new RemoveDependencyCommand(_project.Id, A, B)));
    }

    private AddDependencyCommand Dependency(WorkPackage predecessorPackage, Guid predecessor, WorkPackage successorPackage, Guid successor) =>
        new(_project.Id, predecessorPackage.Id, predecessor, successorPackage.Id, successor, DependencyType.FinishToStart);

    private ScheduleNetwork SeedNetworkWith(Guid predecessor, Guid successor)
    {
        var network = new ScheduleNetwork(_project.Id);
        new ScheduleNetworkService().AddDependency(_project, network, _first, predecessor, _first, successor, DependencyType.FinishToStart);

        return App.Networks.Seed(network);
    }
}
