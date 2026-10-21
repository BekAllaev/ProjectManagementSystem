using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Application.Tests.Projects;

public class CreateProjectTests
{
    [Fact]
    public async Task Handler_ValidCommand_AddsCreatedProjectWithCurrentTime()
    {
        using var app = new TestApplication();
        var handler = new CreateProjectCommandHandler(app.Projects, app.TimeProvider);

        var projectId = await handler.Handle(new CreateProjectCommand("Website", "New website", "eur"), CancellationToken.None);

        var project = Assert.Single(app.Projects.All);
        Assert.Equal(projectId, project.Id);
        Assert.Equal("Website", project.Title);
        Assert.Equal("EUR", project.Currency.Code);
        Assert.Equal(ProjectState.Created, project.State);
        Assert.Equal(app.TimeProvider.GetUtcNow().UtcDateTime, project.CreationDate);
        Assert.Single(project.DomainEvents.OfType<ProjectCreated>());
    }

    [Fact]
    public async Task Pipeline_ValidCommand_SavesOnceAndCollectsProjectCreated()
    {
        using var app = new TestApplication();

        var projectId = await app.SendAsync(new CreateProjectCommand("Website", null, "USD"));

        Assert.Equal(1, app.UnitOfWork.SaveCount);
        var created = Assert.IsType<ProjectCreated>(Assert.Single(app.UnitOfWork.CollectedEvents));
        Assert.Equal(projectId, created.ProjectId);
        Assert.Empty(Assert.Single(app.Projects.All).DomainEvents);
    }
}
