using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class ProjectServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly ProjectService _service = new(new FakeTimeProvider(Now));
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _lead = Guid.NewGuid();
    private readonly Guid _developer = Guid.NewGuid();
    private readonly Project _project;

    public ProjectServiceTests()
    {
        _project = TestData.ProjectWithTeam(_employeeId);
        _project.Start(Now.UtcDateTime.AddDays(-1));
    }

    [Fact]
    public void Project_is_completed_when_every_work_package_is_finished()
    {
        var completed = TestData.WorkPackage(_project, _employeeId);
        var cancelled = TestData.WorkPackage(_project, _employeeId);
        completed.Complete();
        cancelled.Cancel("Not needed");

        _service.Complete(_project, [completed, cancelled]);

        Assert.Equal(ProjectState.Completed, _project.State);
        Assert.Equal(Now.UtcDateTime, _project.CompletionDate);
        Assert.Single(_project.DomainEvents.OfType<ProjectCompleted>());
    }

    [Fact]
    public void Open_work_package_blocks_completion()
    {
        var open = TestData.WorkPackage(_project, _employeeId);

        Assert.Throws<DomainException>(() => _service.Complete(_project, [open]));
        Assert.True(_project.IsOpen);
    }

    [Fact]
    public void Completion_needs_exactly_the_work_packages_of_the_project()
    {
        var open = TestData.WorkPackage(_project, _employeeId);
        var completed = TestData.WorkPackage(_project, _employeeId);
        completed.Complete();
        var otherProject = TestData.ProjectWithTeam(_employeeId);
        var foreign = TestData.WorkPackage(otherProject, _employeeId);
        foreign.Complete();

        Assert.Throws<DomainException>(() => _service.Complete(_project, [completed]));
        Assert.Throws<DomainException>(() => _service.Complete(_project, [open, completed, foreign]));
        Assert.True(_project.IsOpen);
    }

    [Fact]
    public void Only_a_project_in_progress_can_be_completed()
    {
        var created = TestData.ProjectWithTeam(_employeeId);

        Assert.Throws<DomainException>(() => _service.Complete(created, []));
    }

    [Fact]
    public void Responsible_person_of_open_work_cannot_leave_the_team()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        TestData.Task(project, workPackage, _developer);

        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [workPackage], _developer, TestData.Today));
        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [workPackage], _lead, TestData.Today));
    }

    [Fact]
    public void Employee_can_leave_when_their_work_is_finished()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        var task = TestData.Task(project, workPackage, _developer);
        workPackage.CancelTask(task.Id);

        _service.EndTeamMembership(project, [workPackage], _developer, TestData.Today);

        Assert.False(project.IsTeamMemberOn(_developer, TestData.Today.AddDays(1)));
    }

    [Fact]
    public void Leaving_the_team_needs_exactly_the_work_packages_of_the_project()
    {
        var project = TestData.ProjectWithTeam(_lead, _developer);
        var workPackage = TestData.WorkPackage(project, _lead);
        TestData.Task(project, workPackage, _developer);
        var foreign = TestData.WorkPackage(TestData.ProjectWithTeam(_developer), _developer);

        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [], _developer, TestData.Today));
        Assert.Throws<DomainException>(() => _service.EndTeamMembership(project, [workPackage, foreign], _developer, TestData.Today));
        Assert.True(project.IsTeamMemberOn(_developer, TestData.Today.AddDays(1)));
    }
}
