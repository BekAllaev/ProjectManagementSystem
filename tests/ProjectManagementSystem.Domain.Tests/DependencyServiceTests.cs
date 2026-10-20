using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class DependencyServiceTests
{
    private readonly InMemoryWorkPackageRepository _workPackages = new();
    private readonly InMemoryScheduleNetworkRepository _networks = new();
    private readonly DependencyService _service;
    private readonly Guid _employeeId = Guid.NewGuid();

    public DependencyServiceTests() => _service = new DependencyService(_workPackages, _networks);

    [Fact]
    public async Task First_dependency_creates_the_network_of_the_project()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var first = TestData.WorkPackage(project, _employeeId);
        var second = TestData.WorkPackage(project, _employeeId);
        var predecessor = TestData.Task(project, first, _employeeId);
        var successor = TestData.Task(project, second, _employeeId);
        _workPackages.Add(first);
        _workPackages.Add(second);

        await _service.AddDependencyAsync(project.Id, predecessor.Id, successor.Id, DependencyType.FinishToStart);

        var network = await _networks.FindByProjectIdAsync(project.Id);
        var dependency = Assert.Single(network!.Dependencies);
        Assert.Equal(first.Id, dependency.PredecessorWorkPackageId);
        Assert.Equal(second.Id, dependency.SuccessorWorkPackageId);
    }

    [Fact]
    public async Task Cancelled_task_or_task_of_another_project_cannot_take_part()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var otherProject = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var foreignPackage = TestData.WorkPackage(otherProject, _employeeId);
        var task = TestData.Task(project, workPackage, _employeeId);
        var cancelled = TestData.Task(project, workPackage, _employeeId);
        var foreign = TestData.Task(otherProject, foreignPackage, _employeeId);
        workPackage.CancelTask(cancelled.Id);
        _workPackages.Add(workPackage);
        _workPackages.Add(foreignPackage);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.AddDependencyAsync(project.Id, task.Id, cancelled.Id, DependencyType.FinishToStart));
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.AddDependencyAsync(project.Id, task.Id, foreign.Id, DependencyType.FinishToStart));
    }
}
