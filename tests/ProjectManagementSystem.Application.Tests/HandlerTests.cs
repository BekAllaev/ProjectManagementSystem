using ProjectManagementSystem.Application.Schedule;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Schedule.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Tests;

public class HandlerTests
{
    private readonly InMemoryRepository<WorkPackage> _workPackages = new();
    private readonly InMemoryRepository<ScheduleNetwork> _networks = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _employeeId = Guid.NewGuid();

    [Fact]
    public async Task Cancelling_a_work_package_removes_dependencies_of_its_cancelled_tasks()
    {
        var (workPackage, _, _, network) = PackageWithDependency();

        workPackage.Cancel("Out of scope");
        var cancelled = workPackage.DomainEvents.OfType<WorkPackageCancelled>().Single();
        var handler = new RemoveDependenciesOfCancelledWorkPackage(_networks, _unitOfWork);
        await handler.HandleAsync(cancelled);
        await handler.HandleAsync(cancelled);

        Assert.Empty(network.Dependencies);
        Assert.Single(network.DomainEvents.OfType<DependencyRemoved>());
    }

    [Fact]
    public async Task Dependency_added_concurrently_with_cancelling_its_task_is_removed()
    {
        var (workPackage, predecessor, _, network) = PackageWithDependency();
        var added = network.DomainEvents.OfType<DependencyAdded>().Single();

        workPackage.CancelTask(predecessor.Id);
        await new RemoveDependencyOfAlreadyCancelledTask(_workPackages, _networks, _unitOfWork).HandleAsync(added);

        Assert.Empty(network.Dependencies);
        Assert.Single(network.DomainEvents.OfType<PredecessorCancelled>());
    }

    private (WorkPackage, ProjectTask, ProjectTask, ScheduleNetwork) PackageWithDependency()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var predecessor = TestData.Task(project, workPackage, _employeeId);
        var successor = TestData.Task(project, workPackage, _employeeId);
        var network = new ScheduleNetwork(project.Id);
        new ScheduleNetworkService().AddDependency(
            project, network, workPackage, predecessor.Id, workPackage, successor.Id, DependencyType.FinishToStart);
        _workPackages.Add(workPackage);
        _networks.Add(network);

        return (workPackage, predecessor, successor, network);
    }
}
