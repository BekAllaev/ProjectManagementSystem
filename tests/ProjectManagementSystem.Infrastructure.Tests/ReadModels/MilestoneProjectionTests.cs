using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

public sealed class MilestoneProjectionTests : ReadModelTestBase
{
    [Fact]
    public async Task Milestone_keeps_its_tasks_and_their_progress()
    {
        await CreateProjectAsync();
        var auth = await CreateWorkPackageAsync("Auth");
        var login = await CreateTaskAsync(auth, "Login");
        var tokens = await CreateTaskAsync(auth, "Tokens");
        var pentest = await CreateTaskAsync(auth, "Pentest");
        var id = (await SendAndDeliverAsync(new CreateMilestoneCommand(ProjectId, "SSO live", "Everyone signs in with SSO", Today.AddDays(10)))).Id;
        foreach (var task in new[] { login, tokens, pentest })
            await SendAndDeliverAsync(new AddTaskToMilestoneCommand(id, auth, task));
        await SendAndDeliverAsync(new RemoveTaskFromMilestoneCommand(id, pentest));
        await SendAndDeliverAsync(new StartTaskCommand(auth, login));
        await SendAndDeliverAsync(new CompleteTaskCommand(auth, login));
        await SendAndDeliverAsync(new CancelTaskCommand(auth, tokens));
        await SendAndDeliverAsync(new RenameMilestoneCommand(id, "SSO for all"));
        await SendAndDeliverAsync(new ChangeMilestoneTargetDateCommand(id, Today.AddDays(20)));
        await SendAndDeliverAsync(new CompleteMilestoneCommand(id));

        var milestone = Assert.Single(await RowsAsync<MilestoneItemView>());
        Assert.Equal(("SSO for all", "Everyone signs in with SSO", Today.AddDays(20), MilestoneStatus.Completed),
            (milestone.Name, milestone.Description, milestone.TargetDate, milestone.Status));
        Assert.Equal(Clock.GetUtcNow().UtcDateTime, milestone.CompletedAt);
        // The cancelled task stays in the milestone but is not counted.
        Assert.Equal((1, 1, 100), (milestone.CompletedTasks, milestone.CountedTasks, milestone.ProgressPercent));
        Assert.Equal(
            [("Login", "Auth", ProjectTaskStatus.Completed, true), ("Tokens", "Auth", ProjectTaskStatus.Cancelled, false)],
            (await RowsAsync<MilestoneTaskItemView>()).OrderBy(t => t.Name).Select(t => (t.Name, t.WorkPackageDescription, t.Status, t.IsCounted)));
        // Created at version 0, then 3 tasks added, 1 removed, renamed, moved, completed.
        Assert.Equal(7, milestone.Version);
    }

    [Fact]
    public async Task Deleted_milestone_disappears_with_its_tasks()
    {
        await CreateProjectAsync();
        var auth = await CreateWorkPackageAsync("Auth");
        var login = await CreateTaskAsync(auth, "Login");
        var id = (await SendAndDeliverAsync(new CreateMilestoneCommand(ProjectId, "Beta", null, Today))).Id;
        await SendAndDeliverAsync(new AddTaskToMilestoneCommand(id, auth, login));

        await SendAndDeliverAsync(new DeleteMilestoneCommand(id));

        Assert.Empty(await RowsAsync<MilestoneItemView>());
        Assert.Empty(await RowsAsync<MilestoneTaskItemView>());
    }
}
