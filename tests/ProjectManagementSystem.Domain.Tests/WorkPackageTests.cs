using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class WorkPackageTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _otherEmployeeId = Guid.NewGuid();

    [Fact]
    public void Task_cannot_be_added_to_a_package_that_is_not_open()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        workPackage.Complete(_employeeId);

        Assert.Throws<DomainRuleViolationException>(() => TestData.Task(project, workPackage, _employeeId));
    }

    [Fact]
    public void Package_is_completed_only_when_all_tasks_are_finished()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var done = TestData.Task(project, workPackage, _employeeId);
        var dropped = TestData.Task(project, workPackage, _employeeId);

        Assert.Throws<DomainRuleViolationException>(() => workPackage.Complete(_employeeId));

        workPackage.StartTask(done.Id, _employeeId);
        workPackage.CompleteTask(done.Id, _employeeId);
        workPackage.CancelTask(dropped.Id, _employeeId);
        workPackage.Complete(_employeeId);

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
        workPackage.StartTask(completed.Id, _employeeId);
        workPackage.CompleteTask(completed.Id, _employeeId);
        workPackage.StartTask(inProgress.Id, _employeeId);

        workPackage.Cancel(_employeeId, "Out of scope");

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

        Assert.Throws<DomainRuleViolationException>(() => workPackage.CompleteTask(task.Id, _employeeId));

        workPackage.StartTask(task.Id, _employeeId);
        workPackage.CompleteTask(task.Id, _employeeId);

        Assert.Equal(ProjectTaskStatus.Completed, task.Status);
        Assert.Throws<DomainRuleViolationException>(() => workPackage.CancelTask(task.Id, _employeeId));
    }

    [Fact]
    public void Only_the_responsible_person_completes_or_cancels_the_package()
    {
        var project = TestData.ProjectWithTeam(_employeeId, _otherEmployeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);

        Assert.Throws<DomainForbiddenException>(() => workPackage.Complete(_otherEmployeeId));
        Assert.Throws<DomainForbiddenException>(() => workPackage.Cancel(_otherEmployeeId, "Not mine"));
        Assert.Equal(WorkPackageStatus.Open, workPackage.Status);

        workPackage.Cancel(_employeeId, "Out of scope");

        Assert.Equal(WorkPackageStatus.Cancelled, workPackage.Status);
    }

    [Fact]
    public void Responsible_person_of_the_package_completes_it()
    {
        var project = TestData.ProjectWithTeam(_employeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);

        workPackage.Complete(_employeeId);

        Assert.Equal(WorkPackageStatus.Completed, workPackage.Status);
    }

    [Fact]
    public void Status_of_a_task_is_changed_by_its_responsible_person_or_the_responsible_person_of_the_package()
    {
        var taskOwner = Guid.NewGuid();
        var project = TestData.ProjectWithTeam(_employeeId, taskOwner, _otherEmployeeId);
        var workPackage = TestData.WorkPackage(project, _employeeId);
        var first = TestData.Task(project, workPackage, taskOwner);
        var second = TestData.Task(project, workPackage, taskOwner);

        Assert.Throws<DomainForbiddenException>(() => workPackage.StartTask(first.Id, _otherEmployeeId));
        Assert.Throws<DomainForbiddenException>(() => workPackage.CancelTask(first.Id, _otherEmployeeId));
        Assert.Equal(ProjectTaskStatus.Open, first.Status);

        workPackage.StartTask(first.Id, taskOwner);
        Assert.Throws<DomainForbiddenException>(() => workPackage.CompleteTask(first.Id, _otherEmployeeId));
        workPackage.CompleteTask(first.Id, taskOwner);
        workPackage.CancelTask(second.Id, _employeeId);

        Assert.Equal(ProjectTaskStatus.Completed, first.Status);
        Assert.Equal(ProjectTaskStatus.Cancelled, second.Status);
    }
}
