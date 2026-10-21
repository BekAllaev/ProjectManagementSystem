using ProjectManagementSystem.Domain.Projects;
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

        project.Complete();
        Assert.False(project.IsOpen);
    }
}
