using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class ProjectClosureServiceTests
{
    // Project.Start takes the real clock, so the completion must not be earlier than it.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddDays(1);

    private readonly ProjectClosureService _service = new(new FakeTimeProvider(Now));
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Project _project;

    public ProjectClosureServiceTests()
    {
        _project = TestData.ProjectWithTeam(_employeeId);
        _project.Start();
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
    public void Work_packages_must_be_exactly_those_of_the_project()
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
}
