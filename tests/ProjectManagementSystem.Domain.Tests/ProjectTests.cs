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

        project.Start(DateTime.UtcNow);
        Assert.True(project.IsOpen);

        project.Complete(DateTime.UtcNow);
        Assert.False(project.IsOpen);
    }

    [Fact]
    public void Closed_project_gets_no_new_work_packages()
    {
        var project = TestData.ProjectWithTeam();
        project.Start(DateTime.UtcNow);
        project.Complete(DateTime.UtcNow);

        Assert.Throws<DomainException>(() => project.RegisterWorkPackage(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => project.Complete(DateTime.UtcNow));
    }

    [Fact]
    public void Registering_a_work_package_again_changes_nothing()
    {
        var project = TestData.ProjectWithTeam();
        var workPackageId = Guid.NewGuid();
        project.RegisterWorkPackage(workPackageId);
        project.Start(DateTime.UtcNow);
        project.Complete(DateTime.UtcNow);

        project.RegisterWorkPackage(workPackageId);

        Assert.Equal([workPackageId], project.WorkPackageIds);
    }
}
