using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

public sealed class WorkPackageQueryTests : ReadModelTestBase
{
    private Guid _colleague;
    private Guid _auth;
    private Guid _billing;
    private Guid _login;
    private Guid _tokens;
    private Guid _pentest;
    private Guid _invoices;

    [Fact]
    public async Task Work_packages_come_in_the_order_of_registration_with_their_tasks()
    {
        await ArrangeAsync();

        var list = await SendAsync(new GetWorkPackagesQuery(ProjectId));

        Assert.Equal([_auth, _billing], list.Items.Select(w => w.Id));
        Assert.All(list.Items, w => Assert.True(w.IsRegistered));
        var auth = list.Items[0];
        Assert.Equal((1, 3), (auth.CompletedTasks, auth.CountedTasks));
        Assert.Equal(["Login", "Pentest", "Tokens"], auth.Tasks.Select(t => t.Name));
        Assert.Equal(("Jonas", "Weber"), (auth.ResponsiblePerson.FirstName, auth.ResponsiblePerson.LastName));
        Assert.Equal("Tanaka", auth.Tasks.Single(t => t.Name == "Tokens").ResponsiblePerson.LastName);
    }

    [Fact]
    public async Task Tasks_show_logged_hours_the_schedule_warning_and_what_the_caller_may_do()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _tokens, _auth, _pentest, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _login, _billing, _invoices, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new LogTimeCommand(_auth, _tokens, Today.AddDays(-1), 3.5m));
        CurrentUser.EmployeeId = _colleague;
        await SendAndDeliverAsync(new LogTimeCommand(_auth, _tokens, Today, 2m));
        CurrentUser.EmployeeId = Caller;

        var tasks = await SendAsync(new GetTasksQuery(ProjectId, null, null, null));

        Assert.Equal(["Login", "Pentest", "Tokens", "Invoices"], tasks.Items.Select(t => t.Name));
        Assert.Equal((2, 1, 1, 0), (tasks.Counts.Open, tasks.Counts.InProgress, tasks.Counts.Completed, tasks.Counts.Cancelled));
        var byName = tasks.Items.ToDictionary(t => t.Name);
        Assert.Equal(5.5m, byName["Tokens"].LoggedHours);
        // Pentest waits for Tokens, which is in progress; Invoices waits for Login, which is completed.
        Assert.True(byName["Pentest"].HasUnfinishedFinishToStartPredecessor);
        Assert.False(byName["Invoices"].HasUnfinishedFinishToStartPredecessor);
        // The caller is responsible for the package Auth, not for Billing nor its task; Login is finished.
        Assert.Equal([false, true, true, false], tasks.Items.Select(t => t.CanChangeStatus));
    }

    [Fact]
    public async Task Tasks_are_filtered_while_the_counts_stay_those_of_the_project()
    {
        await ArrangeAsync();

        var byResponsible = await SendAsync(new GetTasksQuery(ProjectId, null, null, _colleague));
        var byStatus = await SendAsync(new GetTasksQuery(ProjectId, ProjectTaskStatus.Open, _auth, null));

        Assert.Equal(["Pentest", "Tokens", "Invoices"], byResponsible.Items.Select(t => t.Name));
        Assert.Equal(["Pentest"], byStatus.Items.Select(t => t.Name));
        Assert.Equal(2, byStatus.Counts.Open);
    }

    [Fact]
    public async Task Task_details_show_dependencies_and_the_time_log()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _login, _auth, _tokens, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _auth, _tokens, _billing, _invoices, DependencyType.StartToStart));
        await SendAndDeliverAsync(new LogTimeCommand(_auth, _tokens, Today.AddDays(-2), 4m));
        CurrentUser.EmployeeId = _colleague;
        await SendAndDeliverAsync(new LogTimeCommand(_auth, _tokens, Today, 2m));
        CurrentUser.EmployeeId = Caller;

        var details = await SendAsync(new GetTaskQuery(_auth, _tokens));

        Assert.Equal(("Tokens", ProjectId, "Auth", "EUR"), (details.Name, details.ProjectId, details.WorkPackageDescription, details.Currency));
        var predecessor = Assert.Single(details.Predecessors);
        Assert.Equal((_login, false, DependencyType.FinishToStart, ProjectTaskStatus.Completed),
            (predecessor.TaskId, predecessor.IsInOtherWorkPackage, predecessor.Type, predecessor.Status));
        var successor = Assert.Single(details.Successors);
        Assert.Equal((_invoices, true, DependencyType.StartToStart, "Billing"),
            (successor.TaskId, successor.IsInOtherWorkPackage, successor.Type, successor.WorkPackageDescription));
        Assert.False(details.HasUnfinishedFinishToStartPredecessor);
        Assert.Equal([Today, Today.AddDays(-2)], details.TimeEntries.Select(e => e.WorkDate));
        Assert.Equal([false, true], details.TimeEntries.Select(e => e.CanRemove));
        Assert.Equal((6m, 300m, 1L), (details.LoggedHours, details.LoggedCost, details.TimeLogVersion));
        Assert.True(details.CanChangeStatus);
        Assert.True(details.CanLogTime);
    }

    [Fact]
    public async Task Time_cannot_be_logged_on_an_open_task_nor_by_someone_outside_the_team()
    {
        await ArrangeAsync();

        var open = await SendAsync(new GetTaskQuery(_auth, _pentest));
        CurrentUser.EmployeeId = Guid.NewGuid();
        var outsider = await SendAsync(new GetTaskQuery(_auth, _tokens));

        Assert.Equal((false, true, 0L), (open.CanLogTime, open.CanChangeStatus, open.TimeLogVersion));
        Assert.Equal((false, false), (outsider.CanLogTime, outsider.CanChangeStatus));
    }

    [Fact]
    public async Task Task_of_another_work_package_is_not_found()
    {
        await ArrangeAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => SendAsync(new GetTaskQuery(_billing, _tokens)));
    }

    private async Task ArrangeAsync()
    {
        await CreateProjectAsync();
        _colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(_colleague);
        _auth = await CreateWorkPackageAsync("Auth");
        _billing = await CreateWorkPackageAsync("Billing", _colleague);
        _login = await CreateTaskAsync(_auth, "Login", effort: 24m);
        _tokens = await CreateTaskAsync(_auth, "Tokens", _colleague, 16m);
        _pentest = await CreateTaskAsync(_auth, "Pentest", _colleague);
        _invoices = await CreateTaskAsync(_billing, "Invoices", _colleague);
        await SendAndDeliverAsync(new StartTaskCommand(_auth, _login));
        await SendAndDeliverAsync(new CompleteTaskCommand(_auth, _login));
        await SendAndDeliverAsync(new StartTaskCommand(_auth, _tokens));
    }
}
