using ProjectManagementSystem.Application.Contracts.Events;
using ProjectManagementSystem.Application.Schedule;
using ProjectManagementSystem.Application.Team;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Application.Tests;

public class HandlerTests
{
    private readonly InMemoryWorkPackageRepository _workPackages = new();
    private readonly InMemoryScheduleNetworkRepository _networks = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _employeeId = Guid.NewGuid();

    [Fact]
    public async Task Cancelling_a_work_package_removes_dependencies_of_its_cancelled_tasks()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var predecessor = TestData.Task(project, workPackage, _employeeId);
        var successor = TestData.Task(project, workPackage, _employeeId);
        _workPackages.Add(workPackage);
        await new DependencyService(_workPackages, _networks)
            .AddDependencyAsync(project.Id, predecessor.Id, successor.Id, DependencyType.FinishToStart);

        workPackage.Cancel("Out of scope");
        var cancelled = workPackage.DomainEvents.OfType<WorkPackageCancelled>().Single();
        var handler = new RemoveDependenciesOfCancelledWorkPackage(_networks, _unitOfWork);
        await handler.HandleAsync(cancelled);
        await handler.HandleAsync(cancelled);

        var network = await _networks.FindByProjectIdAsync(project.Id);
        Assert.Empty(network!.Dependencies);
        Assert.Single(network.DomainEvents.OfType<DependencyRemoved>());
    }

    [Fact]
    public async Task Dependency_added_concurrently_with_cancelling_its_task_is_removed()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var predecessor = TestData.Task(project, workPackage, _employeeId);
        var successor = TestData.Task(project, workPackage, _employeeId);
        _workPackages.Add(workPackage);
        await new DependencyService(_workPackages, _networks)
            .AddDependencyAsync(project.Id, predecessor.Id, successor.Id, DependencyType.FinishToStart);
        var network = await _networks.FindByProjectIdAsync(project.Id);
        var added = network!.DomainEvents.OfType<DependencyAdded>().Single();

        workPackage.CancelTask(predecessor.Id);
        await new RemoveDependencyOfAlreadyCancelledTask(_workPackages, _networks, _unitOfWork).HandleAsync(added);

        Assert.Empty(network.Dependencies);
        Assert.Single(network.DomainEvents.OfType<PredecessorCancelled>());
    }

    [Fact]
    public async Task Responsible_person_who_left_the_team_is_reported()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        _workPackages.Add(workPackage);
        var ended = new TeamMembershipEnded(project.Id, Guid.NewGuid(), _employeeId, TestData.Today);
        var publisher = new FakeApplicationEventPublisher();

        await new CheckResponsiblePersonLeftTeam(_workPackages, publisher, _unitOfWork).HandleAsync(ended);

        var reported = Assert.IsType<ResponsiblePersonLeftTeam>(Assert.Single(publisher.Published));
        Assert.Equal(ended.EventId, reported.CausationEventId);
        Assert.Equal([workPackage.Id], reported.WorkPackageIds);
    }
}
