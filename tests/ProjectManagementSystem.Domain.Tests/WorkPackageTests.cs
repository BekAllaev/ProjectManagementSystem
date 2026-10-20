using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class WorkPackageTests
{
    private readonly Guid _employeeId = Guid.NewGuid();

    [Fact]
    public void Task_cannot_be_added_to_a_package_that_is_not_open()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        workPackage.Complete();

        Assert.Throws<DomainException>(() => TestData.Task(project, workPackage, _employeeId));
    }

    [Fact]
    public void Package_is_completed_only_when_all_tasks_are_finished()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var done = TestData.Task(project, workPackage, _employeeId);
        var dropped = TestData.Task(project, workPackage, _employeeId);

        Assert.Throws<DomainException>(workPackage.Complete);

        workPackage.StartTask(done.Id);
        workPackage.CompleteTask(done.Id);
        workPackage.CancelTask(dropped.Id);
        workPackage.Complete();

        Assert.Equal(WorkPackageStatus.Completed, workPackage.Status);
        Assert.Contains(workPackage.DomainEvents, e => e is WorkPackageCompleted);
    }

    [Fact]
    public void Cancel_cancels_unfinished_tasks_and_keeps_completed_ones()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var completed = TestData.Task(project, workPackage, _employeeId);
        var open = TestData.Task(project, workPackage, _employeeId);
        var inProgress = TestData.Task(project, workPackage, _employeeId);
        workPackage.StartTask(completed.Id);
        workPackage.CompleteTask(completed.Id);
        workPackage.StartTask(inProgress.Id);

        workPackage.Cancel("Out of scope");

        Assert.Equal(WorkPackageStatus.Cancelled, workPackage.Status);
        Assert.Equal(ProjectTaskStatus.Completed, completed.Status);
        Assert.Equal(ProjectTaskStatus.Cancelled, open.Status);
        Assert.Equal(ProjectTaskStatus.Cancelled, inProgress.Status);

        var cancelled = Assert.Single(workPackage.DomainEvents.OfType<WorkPackageCancelled>());
        Assert.Equal([open.Id, inProgress.Id], cancelled.CancelledTaskIds);
    }

    [Fact]
    public void Task_goes_through_open_in_progress_completed()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var task = TestData.Task(project, workPackage, _employeeId);

        Assert.Throws<DomainException>(() => workPackage.CompleteTask(task.Id));

        workPackage.StartTask(task.Id);
        workPackage.CompleteTask(task.Id);

        Assert.Equal(ProjectTaskStatus.Completed, task.Status);
        Assert.Throws<DomainException>(() => workPackage.CancelTask(task.Id));
    }
}
