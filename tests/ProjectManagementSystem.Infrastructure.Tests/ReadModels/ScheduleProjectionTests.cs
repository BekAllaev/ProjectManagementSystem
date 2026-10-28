using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

public sealed class ScheduleProjectionTests : ReadModelTestBase
{
    private Guid _auth;
    private Guid _billing;
    private Guid _login;
    private Guid _tokens;
    private Guid _invoices;

    [Fact]
    public async Task Dependencies_keep_the_tasks_their_statuses_and_the_warning()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _login, _auth, _tokens, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _tokens, _billing, _invoices, DependencyType.StartToStart));

        var rows = (await RowsAsync<DependencyItemView>()).OrderBy(d => d.PredecessorName).ToList();
        Assert.Equal([("Login", "Auth", "Tokens", "Auth", false), ("Tokens", "Auth", "Invoices", "Billing", true)],
            rows.Select(d => (d.PredecessorName, d.PredecessorWorkPackageDescription, d.SuccessorName, d.SuccessorWorkPackageDescription, d.IsAcrossWorkPackages)));
        Assert.Equal([true, false], rows.Select(d => d.HasWarning));
        Assert.True(await TaskWarningAsync(_tokens));

        await SendAndDeliverAsync(new StartTaskCommand(_auth, _login));
        await SendAndDeliverAsync(new CompleteTaskCommand(_auth, _login));

        var login = (await RowsAsync<DependencyItemView>()).Single(d => d.PredecessorTaskId == _login);
        Assert.Equal((ProjectTaskStatus.Completed, false), (login.PredecessorStatus, login.HasWarning));
        Assert.False(await TaskWarningAsync(_tokens));
        Assert.Equal(1, Assert.Single(await RowsAsync<ProjectItemView>()).ScheduleVersion);
    }

    [Fact]
    public async Task Removed_and_cancelled_dependencies_disappear()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _login, _auth, _tokens, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _tokens, _billing, _invoices, DependencyType.FinishToStart));

        await SendAndDeliverAsync(new RemoveDependencyCommand(ProjectId, _login, _tokens));
        Assert.False(await TaskWarningAsync(_tokens));

        // Cancelling a task removes its dependencies through the event handlers of the application.
        await SendAndDeliverAsync(new CancelTaskCommand(_auth, _tokens));

        Assert.Empty(await RowsAsync<DependencyItemView>());
        Assert.False(await TaskWarningAsync(_invoices));
    }

    private async Task ArrangeAsync()
    {
        await CreateProjectAsync();
        _auth = await CreateWorkPackageAsync("Auth");
        _billing = await CreateWorkPackageAsync("Billing");
        _login = await CreateTaskAsync(_auth, "Login");
        _tokens = await CreateTaskAsync(_auth, "Tokens");
        _invoices = await CreateTaskAsync(_billing, "Invoices");
    }

    private async Task<bool> TaskWarningAsync(Guid taskId) =>
        (await RowsAsync<TaskItemView>()).Single(t => t.Id == taskId).HasUnfinishedFinishToStartPredecessor;
}
