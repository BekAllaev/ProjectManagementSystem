using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

public sealed class WorkPackageQueryTests : QueryTestBase
{
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _auth;
    private readonly WorkPackage _billing;
    private readonly ProjectTask _login;
    private readonly ProjectTask _tokens;
    private readonly ProjectTask _pentest;
    private readonly ProjectTask _invoices;

    public WorkPackageQueryTests()
    {
        _project = TestData.ProjectWithTeam(Me, _colleague);
        _auth = TestData.WorkPackage(_project, Me);
        _billing = TestData.WorkPackage(_project, _colleague);
        _login = AddTask(_project, _auth, "Login", Me, effort: 24m);
        _tokens = AddTask(_project, _auth, "Tokens", _colleague, effort: 16m);
        _pentest = AddTask(_project, _auth, "Pentest", _colleague);
        _invoices = AddTask(_project, _billing, "Invoices", _colleague);
        _auth.StartTask(_login.Id, Me);
        _auth.CompleteTask(_login.Id, Me);
        _auth.StartTask(_tokens.Id, Me);
    }

    [Fact]
    public async Task Work_packages_come_in_the_order_of_registration_with_an_unregistered_one_last()
    {
        var pending = WorkPackageService.CreateWorkPackage(_project, "Pending", Me);
        var colleague = await EmployeeAsync("Mei", "Tanaka");
        await StoreAsync(colleague, _project, pending, _billing, _auth);

        var list = await SendAsync(new GetWorkPackagesQuery(_project.Id));

        Assert.Equal([_auth.Id, _billing.Id, pending.Id], list.Items.Select(w => w.Id));
        Assert.Equal([true, true, false], list.Items.Select(w => w.IsRegistered));
        var auth = list.Items[0];
        Assert.Equal((1, 3), (auth.CompletedTasks, auth.CountedTasks));
        Assert.Equal(["Login", "Pentest", "Tokens"], auth.Tasks.Select(t => t.Name));
        Assert.Equal(Me, auth.ResponsiblePerson.Id);
        Assert.Equal(string.Empty, auth.ResponsiblePerson.LastName);
        Assert.Empty(list.Items[2].Tasks);
    }

    [Fact]
    public async Task Tasks_show_logged_hours_the_schedule_warning_and_what_the_caller_may_do()
    {
        var network = new ScheduleNetwork(_project.Id);
        new ScheduleNetworkService().AddDependency(_project, network, _auth, _tokens.Id, _auth, _pentest.Id, DependencyType.FinishToStart);
        new ScheduleNetworkService().AddDependency(_project, network, _auth, _login.Id, _billing, _invoices.Id, DependencyType.FinishToStart);
        var timeLogs = new TaskTimeLogService(TestData.TimeProvider);
        var timeLog = timeLogs.CreateTimeLog(_auth, _tokens.Id);
        timeLogs.LogTime(_project, _auth, timeLog, Me, Today.AddDays(-1), 3.5m);
        timeLogs.LogTime(_project, _auth, timeLog, _colleague, Today, 2m);
        await StoreAsync(_project, _auth, _billing, network, timeLog);

        var tasks = await SendAsync(new GetTasksQuery(_project.Id, null, null, null));

        Assert.Equal(["Login", "Pentest", "Tokens", "Invoices"], tasks.Items.Select(t => t.Name));
        Assert.Equal((2, 1, 1, 0), (tasks.Counts.Open, tasks.Counts.InProgress, tasks.Counts.Completed, tasks.Counts.Cancelled));
        var byName = tasks.Items.ToDictionary(t => t.Name);
        Assert.Equal(5.5m, byName["Tokens"].LoggedHours);
        Assert.Equal(0m, byName["Pentest"].LoggedHours);
        // Pentest waits for Tokens, which is in progress; Invoices waits for Login, which is completed.
        Assert.True(byName["Pentest"].HasUnfinishedFinishToStartPredecessor);
        Assert.False(byName["Invoices"].HasUnfinishedFinishToStartPredecessor);
        // The caller is responsible for the package Auth, not for Billing nor its task; Login is finished.
        Assert.Equal([false, true, true, false], tasks.Items.Select(t => t.CanChangeStatus));
    }

    [Fact]
    public async Task Tasks_are_filtered_while_the_counts_stay_those_of_the_project()
    {
        await StoreAsync(_project, _auth, _billing);

        var byResponsible = await SendAsync(new GetTasksQuery(_project.Id, null, null, _colleague));
        var byStatus = await SendAsync(new GetTasksQuery(_project.Id, ProjectTaskStatus.Open, _auth.Id, null));

        Assert.Equal(["Pentest", "Tokens", "Invoices"], byResponsible.Items.Select(t => t.Name));
        Assert.Equal(["Pentest"], byStatus.Items.Select(t => t.Name));
        Assert.Equal(2, byStatus.Counts.Open);
    }

    [Fact]
    public async Task Task_details_show_dependencies_and_the_time_log()
    {
        var network = new ScheduleNetwork(_project.Id);
        new ScheduleNetworkService().AddDependency(_project, network, _auth, _login.Id, _auth, _tokens.Id, DependencyType.FinishToStart);
        new ScheduleNetworkService().AddDependency(_project, network, _auth, _tokens.Id, _billing, _invoices.Id, DependencyType.StartToStart);
        var timeLogs = new TaskTimeLogService(TestData.TimeProvider);
        var timeLog = timeLogs.CreateTimeLog(_auth, _tokens.Id);
        timeLogs.LogTime(_project, _auth, timeLog, Me, Today.AddDays(-2), 4m);
        timeLogs.LogTime(_project, _auth, timeLog, _colleague, Today, 2m);
        await StoreAsync(_project, _auth, _billing, network, timeLog);

        var details = await SendAsync(new GetTaskQuery(_auth.Id, _tokens.Id));

        Assert.Equal(("Tokens", _project.Id, "Work package", "USD"), (details.Name, details.ProjectId, details.WorkPackageDescription, details.Currency));
        Assert.Equal(ProjectTaskStatus.InProgress, details.Status);
        var predecessor = Assert.Single(details.Predecessors);
        Assert.Equal((_login.Id, false, DependencyType.FinishToStart, ProjectTaskStatus.Completed),
            (predecessor.TaskId, predecessor.IsInOtherWorkPackage, predecessor.Type, predecessor.Status));
        var successor = Assert.Single(details.Successors);
        Assert.Equal((_invoices.Id, true, DependencyType.StartToStart), (successor.TaskId, successor.IsInOtherWorkPackage, successor.Type));
        Assert.False(details.HasUnfinishedFinishToStartPredecessor);

        Assert.Equal([Today, Today.AddDays(-2)], details.TimeEntries.Select(e => e.WorkDate));
        Assert.Equal([false, true], details.TimeEntries.Select(e => e.CanRemove));
        Assert.Equal((6m, 300m), (details.LoggedHours, details.LoggedCost));
        Assert.Equal(200m, details.TimeEntries[1].Cost);
        Assert.True(details.CanChangeStatus);
        Assert.True(details.CanLogTime);
    }

    [Fact]
    public async Task Time_cannot_be_logged_on_an_open_task_nor_by_someone_outside_the_team()
    {
        await StoreAsync(_project, _auth, _billing);

        var open = await SendAsync(new GetTaskQuery(_auth.Id, _pentest.Id));
        CurrentUser.EmployeeId = Guid.NewGuid();
        var outsider = await SendAsync(new GetTaskQuery(_auth.Id, _tokens.Id));

        Assert.False(open.CanLogTime);
        Assert.True(open.CanChangeStatus);
        Assert.Equal(0, open.TimeLogVersion);
        Assert.False(outsider.CanLogTime);
        Assert.False(outsider.CanChangeStatus);
    }

    [Fact]
    public async Task Task_of_another_work_package_is_not_found()
    {
        await StoreAsync(_project, _auth, _billing);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => SendAsync(new GetTaskQuery(_billing.Id, _tokens.Id)));
    }
}
