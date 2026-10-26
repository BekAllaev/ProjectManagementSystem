using ProjectManagementSystem.Application.Projects.Commands.CloseProject;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.Projects.EventHandlers;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Tests.Projects;

/// <summary>
/// Showcase of eventual consistency: a new work package is registered in its project by the handler of
/// <see cref="WorkPackageCreated"/>, with a compensation if the project was closed in the meantime.
/// </summary>
public class WorkPackageRegistrationTests : CommandTestBase
{
    [Fact]
    public async Task CreateWorkPackage_ClosedProject_IsRejectedByEntryCheck()
    {
        var project = SeedProject();
        TestData.Close(project);
        project.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member)));
        Assert.Empty(App.WorkPackages.All);
    }

    [Fact]
    public async Task Delivery_OpenProject_RegistersWorkPackageInProject()
    {
        var project = SeedProject();
        var workPackageId = await App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member));
        Assert.Empty(project.WorkPackageIds);

        await App.DeliverEventsAsync();

        Assert.Equal([workPackageId], project.WorkPackageIds);
        Assert.Equal(WorkPackageStatus.Open, App.WorkPackages.All.Single().Status);
    }

    [Fact]
    public async Task Delivery_RegisteredOpenWorkPackage_BlocksClosingTheProject()
    {
        var project = SeedProject();
        await App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member));
        await App.SendAsync(new StartProjectCommand(project.Id));
        await App.DeliverEventsAsync();

        await Assert.ThrowsAsync<DomainRuleViolationException>(() => App.SendAsync(new CloseProjectCommand(project.Id)));
        Assert.True(project.IsOpen);
    }

    [Fact]
    public async Task Delivery_ProjectClosedBeforeDelivery_CancelsWorkPackageAndProjectStaysClosed()
    {
        var project = SeedProject();
        var workPackageId = await App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member));
        var taskId = await App.SendAsync(new CreateTaskCommand(workPackageId, "Mockups", null, Member, 8m));
        await App.SendAsync(new StartProjectCommand(project.Id));
        await App.SendAsync(new CloseProjectCommand(project.Id));

        var delivered = await App.DeliverEventsAsync();

        var workPackage = App.WorkPackages.All.Single();
        Assert.Equal(WorkPackageStatus.Cancelled, workPackage.Status);
        Assert.Equal(RegisterWorkPackageInProject.ProjectClosedReason, workPackage.CancellationReason);
        Assert.Equal(ProjectTaskStatus.Cancelled, workPackage.GetTask(taskId).Status);
        Assert.Equal(ProjectState.Completed, project.State);
        Assert.Empty(project.WorkPackageIds);
        var cancelled = Assert.Single(delivered.OfType<WorkPackageCancelled>());
        Assert.Equal([taskId], cancelled.CancelledTaskIds);
    }

    [Fact]
    public async Task Redelivery_AfterRegistration_ChangesNothing()
    {
        var project = SeedProject();
        var workPackageId = await App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member));
        var delivered = await App.DeliverEventsAsync();
        var eventCount = App.UnitOfWork.CollectedEvents.Count;
        var saveCount = App.UnitOfWork.SaveCount;

        await App.RedeliverAsync(delivered);

        Assert.Equal([workPackageId], project.WorkPackageIds);
        Assert.Equal(eventCount, App.UnitOfWork.CollectedEvents.Count);
        Assert.Equal(saveCount, App.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Redelivery_AfterCompensation_ChangesNothing()
    {
        var project = SeedProject();
        await App.SendAsync(new CreateWorkPackageCommand(project.Id, "Design", Member));
        await App.SendAsync(new StartProjectCommand(project.Id));
        await App.SendAsync(new CloseProjectCommand(project.Id));
        var delivered = await App.DeliverEventsAsync();
        var eventCount = App.UnitOfWork.CollectedEvents.Count;

        await App.RedeliverAsync(delivered);

        Assert.Equal(WorkPackageStatus.Cancelled, App.WorkPackages.All.Single().Status);
        Assert.Empty(project.WorkPackageIds);
        Assert.Equal(eventCount, App.UnitOfWork.CollectedEvents.Count);
    }
}
