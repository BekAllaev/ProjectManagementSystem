using ProjectManagementSystem.Domain.Projects.Events;
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

        project.Start(TestData.Now);
        Assert.True(project.IsOpen);

        project.Complete(TestData.Now);
        Assert.False(project.IsOpen);
    }

    [Fact]
    public void Closed_project_gets_no_new_work_packages()
    {
        var project = TestData.ProjectWithTeam();
        project.Start(TestData.Now);
        project.Complete(TestData.Now);

        Assert.Throws<DomainRuleViolationException>(() => project.RegisterWorkPackage(Guid.NewGuid()));
        Assert.Throws<DomainRuleViolationException>(() => project.Complete(TestData.Now));
    }

    [Fact]
    public void Registering_a_work_package_again_changes_nothing()
    {
        var project = TestData.ProjectWithTeam();
        var workPackageId = Guid.NewGuid();
        project.RegisterWorkPackage(workPackageId);
        project.Start(TestData.Now);
        project.Complete(TestData.Now);

        project.ClearDomainEvents();

        project.RegisterWorkPackage(workPackageId);

        Assert.Equal([workPackageId], project.WorkPackageIds);
        Assert.Empty(project.DomainEvents);
    }

    [Fact]
    public void Registering_a_work_package_raises_an_event()
    {
        var project = TestData.ProjectWithTeam();
        project.ClearDomainEvents();
        var workPackageId = Guid.NewGuid();

        project.RegisterWorkPackage(workPackageId);

        Assert.Equal(new WorkPackageRegistered(project.Id, workPackageId), Assert.Single(project.DomainEvents));
    }
}
