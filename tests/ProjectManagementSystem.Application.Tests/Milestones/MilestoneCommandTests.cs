using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Milestones.Events;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Tests.Milestones;

public class MilestoneCommandTests : CommandTestBase
{
    private readonly Project _project;
    private readonly WorkPackage _workPackage;

    public MilestoneCommandTests()
    {
        _project = SeedProject();
        _workPackage = SeedWorkPackage(_project, taskCount: 1);
    }

    private Guid TaskId => _workPackage.Tasks.Single().Id;

    [Fact]
    public async Task CreateMilestone_OpenProject_AddsOpenMilestone()
    {
        var milestoneId = await App.SendAsync(new CreateMilestoneCommand(_project.Id, "Beta", null, Today.AddDays(30)));

        var milestone = Assert.Single(App.Milestones.All);
        Assert.Equal(milestoneId, milestone.Id);
        Assert.Equal(MilestoneStatus.Open, milestone.Status);
        Assert.Equal(Today.AddDays(30), milestone.TargetDate);
        AssertCommitted(milestone, typeof(MilestoneCreated));
    }

    [Fact]
    public async Task CreateMilestone_ClosedProject_ThrowsDomainException()
    {
        TestData.Close(_project);
        _project.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() =>
            App.SendAsync(new CreateMilestoneCommand(_project.Id, "Beta", null, Today)));
        Assert.Empty(App.Milestones.All);
    }

    [Fact]
    public async Task AddTaskToMilestone_TaskOfProject_AddsIt()
    {
        var milestone = SeedMilestone();

        await App.SendAsync(new AddTaskToMilestoneCommand(milestone.Id, _workPackage.Id, TaskId));

        Assert.Equal([TaskId], milestone.TaskIds);
        AssertCommitted(milestone, typeof(MilestoneTaskAdded));
    }

    [Fact]
    public async Task AddTaskToMilestone_CancelledTask_ThrowsDomainException()
    {
        var milestone = SeedMilestone();
        _workPackage.CancelTask(TaskId);
        _workPackage.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() =>
            App.SendAsync(new AddTaskToMilestoneCommand(milestone.Id, _workPackage.Id, TaskId)));
        Assert.Empty(milestone.TaskIds);
    }

    [Fact]
    public async Task RemoveTaskFromMilestone_TaskInMilestone_RemovesIt()
    {
        var milestone = SeedMilestone(withTask: true);

        await App.SendAsync(new RemoveTaskFromMilestoneCommand(milestone.Id, TaskId));

        Assert.Empty(milestone.TaskIds);
        AssertCommitted(milestone, typeof(MilestoneTaskRemoved));
    }

    [Fact]
    public async Task RenameMilestone_OpenMilestone_RenamesIt()
    {
        var milestone = SeedMilestone();

        await App.SendAsync(new RenameMilestoneCommand(milestone.Id, "Release candidate"));

        Assert.Equal("Release candidate", milestone.Name);
        AssertCommitted(milestone, typeof(MilestoneRenamed));
    }

    [Fact]
    public async Task ChangeMilestoneTargetDate_OpenMilestone_ChangesIt()
    {
        var milestone = SeedMilestone();

        await App.SendAsync(new ChangeMilestoneTargetDateCommand(milestone.Id, Today.AddDays(60)));

        Assert.Equal(Today.AddDays(60), milestone.TargetDate);
        AssertCommitted(milestone, typeof(MilestoneTargetDateChanged));
    }

    [Fact]
    public async Task CompleteMilestone_OpenMilestone_CompletesItAtCurrentTime()
    {
        var milestone = SeedMilestone(withTask: true);
        App.TimeProvider.Advance(TimeSpan.FromHours(5));

        await App.SendAsync(new CompleteMilestoneCommand(milestone.Id));

        Assert.Equal(MilestoneStatus.Completed, milestone.Status);
        Assert.Equal(Now, milestone.CompletedAt);
        AssertCommitted(milestone, typeof(MilestoneCompleted));
    }

    [Fact]
    public async Task RenameMilestone_CompletedMilestone_ThrowsDomainException()
    {
        var milestone = SeedMilestone();
        milestone.Complete(Now);
        milestone.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(new RenameMilestoneCommand(milestone.Id, "Late rename")));
    }

    [Fact]
    public async Task DeleteMilestone_CompletedMilestone_RemovesItFromRepository()
    {
        var milestone = SeedMilestone();
        milestone.Complete(Now);
        milestone.ClearDomainEvents();

        await App.SendAsync(new DeleteMilestoneCommand(milestone.Id));

        Assert.Empty(App.Milestones.All);
        AssertCommitted(milestone, typeof(MilestoneDeleted));
    }

    private Milestone SeedMilestone(bool withTask = false)
    {
        var service = new MilestoneService();
        var milestone = service.Create(_project, "Beta", "First public version", Today.AddDays(30));
        if (withTask)
            service.AddTask(_project, milestone, _workPackage, TaskId);

        return App.Milestones.Seed(milestone);
    }
}
