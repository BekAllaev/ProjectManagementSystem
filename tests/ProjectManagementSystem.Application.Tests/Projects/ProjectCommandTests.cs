using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Projects.Commands.CloseProject;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Application.Tests.Projects;

public class ProjectCommandTests : CommandTestBase
{
    [Fact]
    public async Task StartProject_CreatedProject_StartsAtCurrentTime()
    {
        var project = SeedProject();

        await App.SendAsync(new StartProjectCommand(project.Id));

        Assert.Equal(ProjectState.InProgress, project.State);
        Assert.Equal(Now, project.StartDate);
        AssertCommitted(project, typeof(ProjectStarted));
    }

    [Fact]
    public async Task StartProject_AlreadyStarted_ThrowsDomainException()
    {
        var project = SeedProject();
        project.Start(Now);
        project.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(new StartProjectCommand(project.Id)));
    }

    [Fact]
    public async Task StartProject_UnknownProject_ThrowsNotFoundException()
    {
        await AssertRejectedAsync<NotFoundException>(() => App.SendAsync(new StartProjectCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task CloseProject_AllWorkPackagesFinished_CompletesProjectAtCurrentTime()
    {
        var project = SeedProject();
        project.Start(Now.AddDays(-1));
        project.ClearDomainEvents();
        var completed = SeedWorkPackage(project);
        completed.Complete();
        var cancelled = SeedWorkPackage(project, taskCount: 1);
        cancelled.Cancel("Out of scope");
        completed.ClearDomainEvents();
        cancelled.ClearDomainEvents();

        await App.SendAsync(new CloseProjectCommand(project.Id));

        Assert.Equal(ProjectState.Completed, project.State);
        Assert.Equal(Now, project.CompletionDate);
        AssertCommitted(project, typeof(ProjectCompleted));
    }

    [Fact]
    public async Task CloseProject_OpenWorkPackage_ThrowsDomainException()
    {
        var project = SeedProject();
        project.Start(Now.AddDays(-1));
        project.ClearDomainEvents();
        SeedWorkPackage(project);

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(new CloseProjectCommand(project.Id)));
        Assert.True(project.IsOpen);
    }
}
