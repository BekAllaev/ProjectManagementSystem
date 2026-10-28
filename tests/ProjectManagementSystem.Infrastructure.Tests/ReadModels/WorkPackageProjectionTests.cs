using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

public sealed class WorkPackageProjectionTests : ReadModelTestBase
{
    [Fact]
    public async Task Tasks_their_package_and_the_project_follow_the_events()
    {
        await CreateProjectAsync("Portal");
        var auth = await CreateWorkPackageAsync("Auth");
        var login = await CreateTaskAsync(auth, "Login", effort: 24m);
        var tokens = await CreateTaskAsync(auth, "Tokens");
        var pentest = await CreateTaskAsync(auth, "Pentest");
        await SendAndDeliverAsync(new StartTaskCommand(auth, login));
        await SendAndDeliverAsync(new CompleteTaskCommand(auth, login));
        await SendAndDeliverAsync(new StartTaskCommand(auth, tokens));
        await SendAndDeliverAsync(new CancelTaskCommand(auth, pentest));

        var tasks = (await RowsAsync<TaskItemView>()).ToDictionary(t => t.Name);
        Assert.Equal(
            [ProjectTaskStatus.Completed, ProjectTaskStatus.InProgress, ProjectTaskStatus.Cancelled],
            new[] { "Login", "Tokens", "Pentest" }.Select(n => tasks[n].Status));
        Assert.Equal(("Portal", "Auth", Caller, 24m, "About Login"),
            (tasks["Login"].ProjectTitle, tasks["Login"].WorkPackageDescription, tasks["Login"].ResponsiblePersonId,
                tasks["Login"].EstimatedEffort, tasks["Login"].Description));

        var workPackage = Assert.Single(await RowsAsync<WorkPackageItemView>());
        Assert.Equal((true, 1, 2), (workPackage.IsRegistered, workPackage.CompletedTasks, workPackage.CountedTasks));
        // Created at version 0, then 3 tasks and 4 changes of status.
        Assert.Equal(7, workPackage.Version);
        Assert.All(tasks.Values, t => Assert.Equal(7, t.WorkPackageVersion));

        var project = Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Equal((0, 1, 1, 1, 2, 50),
            (project.OpenTasks, project.InProgressTasks, project.CompletedTasks, project.CancelledTasks, project.CountedTasks, project.ProgressPercent));
    }

    [Fact]
    public async Task Cancelling_a_package_cancels_its_unfinished_tasks()
    {
        await CreateProjectAsync();
        var billing = await CreateWorkPackageAsync("Billing");
        var invoices = await CreateTaskAsync(billing, "Invoices");
        await CreateTaskAsync(billing, "Reminders");
        await SendAndDeliverAsync(new StartTaskCommand(billing, invoices));
        await SendAndDeliverAsync(new CompleteTaskCommand(billing, invoices));

        await SendAndDeliverAsync(new CancelWorkPackageCommand(billing, "Out of scope"));

        var workPackage = Assert.Single(await RowsAsync<WorkPackageItemView>());
        Assert.Equal((WorkPackageStatus.Cancelled, "Out of scope", 1, 1),
            (workPackage.Status, workPackage.CancellationReason, workPackage.CompletedTasks, workPackage.CountedTasks));
        Assert.Equal(
            [("Invoices", ProjectTaskStatus.Completed), ("Reminders", ProjectTaskStatus.Cancelled)],
            (await RowsAsync<TaskItemView>()).OrderBy(t => t.Name).Select(t => (t.Name, t.Status)));
    }

    [Fact]
    public async Task Responsible_persons_follow_the_changes()
    {
        await CreateProjectAsync();
        var colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(colleague);
        var auth = await CreateWorkPackageAsync("Auth");
        var login = await CreateTaskAsync(auth, "Login");
        var tokens = await CreateTaskAsync(auth, "Tokens");

        await SendAndDeliverAsync(new ChangeTaskResponsibleCommand(auth, login, colleague));
        await SendAndDeliverAsync(new ChangeWorkPackageResponsibleCommand(auth, colleague));

        Assert.Equal(colleague, Assert.Single(await RowsAsync<WorkPackageItemView>()).ResponsiblePersonId);
        var tasks = (await RowsAsync<TaskItemView>()).ToDictionary(t => t.Id);
        Assert.Equal((colleague, Caller), (tasks[login].ResponsiblePersonId, tasks[tokens].ResponsiblePersonId));
        Assert.All(tasks.Values, t => Assert.Equal(colleague, t.WorkPackageResponsiblePersonId));
    }
}
