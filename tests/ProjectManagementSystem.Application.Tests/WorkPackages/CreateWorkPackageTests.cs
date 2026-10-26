using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Tests.Fakes;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Tests.WorkPackages;

public class CreateWorkPackageTests
{
    private readonly TestApplication _app = new();
    private readonly Guid _member = Guid.NewGuid();

    [Fact]
    public async Task Handler_ValidCommand_AddsOpenWorkPackageAndLeavesProjectUnchanged()
    {
        var project = _app.Projects.Seed(TestData.ProjectWithTeam(_member));

        var workPackageId = await CreateHandler().Handle(
            new CreateWorkPackageCommand(project.Id, "Design", _member), CancellationToken.None);

        var workPackage = Assert.Single(_app.WorkPackages.All);
        Assert.Equal(workPackageId, workPackage.Id);
        Assert.Equal(project.Id, workPackage.ProjectId);
        Assert.Equal(_member, workPackage.ResponsiblePersonId);
        Assert.Equal(WorkPackageStatus.Open, workPackage.Status);
        Assert.Single(workPackage.DomainEvents.OfType<WorkPackageCreated>());
        Assert.Empty(project.WorkPackageIds);
        Assert.Empty(project.DomainEvents);
    }

    [Fact]
    public async Task Pipeline_ValidCommand_SavesOnceAndCollectsOnlyWorkPackageCreated()
    {
        var project = _app.Projects.Seed(TestData.ProjectWithTeam(_member));

        var workPackageId = await _app.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", _member));

        Assert.Equal(1, _app.UnitOfWork.SaveCount);
        Assert.Same(_app.WorkPackages.All.Single(), Assert.Single(_app.UnitOfWork.ChangedAggregates));
        var created = Assert.IsType<WorkPackageCreated>(Assert.Single(_app.UnitOfWork.CollectedEvents));
        Assert.Equal(workPackageId, created.WorkPackageId);
        Assert.Equal(project.Id, created.ProjectId);
    }

    [Fact]
    public async Task Pipeline_ClosedProject_ThrowsDomainRuleViolationExceptionAndSavesNothing()
    {
        var project = TestData.ProjectWithTeam(_member);
        TestData.Close(project);
        _app.Projects.Seed(project);

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            _app.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", _member)));

        Assert.Empty(_app.WorkPackages.All);
        Assert.Equal(0, _app.UnitOfWork.SaveCount);
        Assert.Empty(_app.UnitOfWork.CollectedEvents);
    }

    [Fact]
    public async Task Pipeline_UnknownProject_ThrowsAggregateNotFoundExceptionAndSavesNothing()
    {
        var projectId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<AggregateNotFoundException>(() =>
            _app.SendAsync(new CreateWorkPackageCommand(projectId, "Design", _member)));

        Assert.Equal(projectId, exception.Id);
        Assert.Empty(_app.WorkPackages.All);
        Assert.Equal(0, _app.UnitOfWork.SaveCount);
    }

    private CreateWorkPackageCommandHandler CreateHandler() =>
        new(_app.Projects, _app.WorkPackages, new WorkPackageService(_app.TimeProvider));
}
