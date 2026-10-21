using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void Created_and_in_progress_projects_are_open()
    {
        var project = TestData.ProjectWithTeam();
        Assert.True(project.IsOpen);

        project.Start();
        Assert.True(project.IsOpen);

        project.Complete(DateTime.UtcNow);
        Assert.False(project.IsOpen);
    }

    [Fact]
    public void Closed_project_gets_no_new_work_packages()
    {
        var project = TestData.ProjectWithTeam();
        project.Start();
        project.Complete(DateTime.UtcNow);

        Assert.Throws<DomainException>(() => project.RegisterWorkPackage(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => project.Complete(DateTime.UtcNow));
    }
}
