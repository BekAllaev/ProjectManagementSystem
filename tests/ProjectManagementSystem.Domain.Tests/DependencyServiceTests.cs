using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class DependencyServiceTests
{
    private readonly DependencyService _service = new();
    private readonly Guid _employeeId = Guid.NewGuid();

    [Fact]
    public void Dependency_keeps_the_work_packages_of_its_tasks()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var network = new ScheduleNetwork(project.Id);
        var first = TestData.WorkPackage(project, _employeeId);
        var second = TestData.WorkPackage(project, _employeeId);
        var predecessor = TestData.Task(project, first, _employeeId);
        var successor = TestData.Task(project, second, _employeeId);

        _service.AddDependency(network, first, predecessor.Id, second, successor.Id, DependencyType.FinishToStart);

        var dependency = Assert.Single(network.Dependencies);
        Assert.Equal(first.Id, dependency.PredecessorWorkPackageId);
        Assert.Equal(second.Id, dependency.SuccessorWorkPackageId);
    }

    [Fact]
    public void Cancelled_task_or_task_of_another_project_cannot_take_part()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var otherProject = TestData.ProjectWithTeam(_employeeId);
        var network = new ScheduleNetwork(project.Id);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var foreignPackage = TestData.WorkPackage(otherProject, _employeeId);
        var task = TestData.Task(project, workPackage, _employeeId);
        var cancelled = TestData.Task(project, workPackage, _employeeId);
        var foreign = TestData.Task(otherProject, foreignPackage, _employeeId);
        workPackage.CancelTask(cancelled.Id);

        Assert.Throws<DomainException>(() =>
            _service.AddDependency(network, workPackage, task.Id, workPackage, cancelled.Id, DependencyType.FinishToStart));
        Assert.Throws<DomainException>(() =>
            _service.AddDependency(network, workPackage, task.Id, foreignPackage, foreign.Id, DependencyType.FinishToStart));
        Assert.Empty(network.Dependencies);
    }
}
