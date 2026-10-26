using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Milestones.Events;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Tests;

public class MilestoneServiceTests
{
    private readonly MilestoneService _service = new();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _workPackage;

    public MilestoneServiceTests()
    {
        _project = TestData.ProjectWithTeam(_employeeId);
        _workPackage = TestData.WorkPackage(_project, _employeeId);
    }

    [Fact]
    public void Milestone_is_created_only_in_an_open_project()
    {
        var milestone = _service.Create(_project, "Release", "First release", TestData.Today);
        Assert.Equal(_project.Id, milestone.ProjectId);

        TestData.Close(_project);

        Assert.Throws<DomainRuleViolationException>(() => _service.Create(_project, "Release", "", TestData.Today));
    }

    [Fact]
    public void Open_and_completed_tasks_can_be_added_cancelled_and_missing_ones_cannot()
    {
        var milestone = _service.Create(_project, "Release", "", TestData.Today);
        var open = TestData.Task(_project, _workPackage, _employeeId);
        var completed = TestData.Task(_project, _workPackage, _employeeId);
        var cancelled = TestData.Task(_project, _workPackage, _employeeId);
        _workPackage.StartTask(completed.Id);
        _workPackage.CompleteTask(completed.Id);
        _workPackage.CancelTask(cancelled.Id);

        _service.AddTask(_project, milestone, _workPackage, open.Id);
        _service.AddTask(_project, milestone, _workPackage, completed.Id);

        Assert.Throws<DomainRuleViolationException>(() => _service.AddTask(_project, milestone, _workPackage, cancelled.Id));
        Assert.Throws<EntityNotFoundException>(() => _service.AddTask(_project, milestone, _workPackage, Guid.NewGuid()));
        Assert.Equal([open.Id, completed.Id], milestone.TaskIds);
        Assert.Equal(2, milestone.DomainEvents.OfType<MilestoneTaskAdded>().Count());
    }

    [Fact]
    public void Milestone_and_work_package_must_belong_to_the_project()
    {
        var otherProject = TestData.ProjectWithTeam(_employeeId);
        var foreignPackage = TestData.WorkPackage(otherProject, _employeeId);
        var foreignTask = TestData.Task(otherProject, foreignPackage, _employeeId);
        var foreignMilestone = _service.Create(otherProject, "Other", "", TestData.Today);
        var milestone = _service.Create(_project, "Release", "", TestData.Today);
        var task = TestData.Task(_project, _workPackage, _employeeId);

        Assert.Throws<DomainValidationException>(() => _service.AddTask(_project, foreignMilestone, _workPackage, task.Id));
        Assert.Throws<DomainValidationException>(() => _service.AddTask(_project, milestone, foreignPackage, foreignTask.Id));
        Assert.Empty(milestone.TaskIds);
    }

    [Fact]
    public void Closed_project_gets_no_new_milestone_tasks()
    {
        var milestone = _service.Create(_project, "Release", "", TestData.Today);
        var task = TestData.Task(_project, _workPackage, _employeeId);
        _workPackage.StartTask(task.Id);
        _workPackage.CompleteTask(task.Id);
        _workPackage.Complete();

        TestData.Close(_project);

        Assert.Throws<DomainRuleViolationException>(() => _service.AddTask(_project, milestone, _workPackage, task.Id));
    }
}
