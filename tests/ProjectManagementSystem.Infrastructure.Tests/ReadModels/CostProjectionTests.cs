using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

/// <summary>
/// Time entries and funding, and the figures summed from them.
/// </summary>
public sealed class CostProjectionTests : ReadModelTestBase
{
    [Fact]
    public async Task Logged_time_is_summed_on_the_task_the_project_and_by_member_and_package()
    {
        await CreateProjectAsync();
        var colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(colleague);
        var auth = await CreateWorkPackageAsync("Auth");
        var billing = await CreateWorkPackageAsync("Billing");
        var login = await CreateTaskAsync(auth, "Login");
        var invoices = await CreateTaskAsync(billing, "Invoices", colleague);
        await SendAndDeliverAsync(new StartTaskCommand(auth, login));
        await SendAndDeliverAsync(new StartTaskCommand(billing, invoices));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 800m, Today.AddDays(-10), "Budget"));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 200m, Today, null));

        // Every member has the rate 50.
        await SendAndDeliverAsync(new LogTimeCommand(auth, login, Today.AddDays(-1), 4m));
        var wrong = (await SendAndDeliverAsync(new LogTimeCommand(auth, login, Today, 9m))).Id;
        CurrentUser.EmployeeId = colleague;
        await SendAndDeliverAsync(new LogTimeCommand(billing, invoices, Today, 6m));
        CurrentUser.EmployeeId = Caller;
        await SendAndDeliverAsync(new RemoveTimeEntryCommand(login, wrong));

        Assert.Equal([4m, 6m], (await RowsAsync<TimeEntryItemView>()).OrderBy(e => e.Hours).Select(e => e.Hours));
        var tasks = (await RowsAsync<TaskItemView>()).ToDictionary(t => t.Id);
        Assert.Equal((4m, 200m, 2L), (tasks[login].LoggedHours, tasks[login].LoggedCost, tasks[login].TimeLogVersion));
        Assert.Equal((6m, 300m, 0L), (tasks[invoices].LoggedHours, tasks[invoices].LoggedCost, tasks[invoices].TimeLogVersion));

        var project = Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Equal((1000m, 500m, 500m, 50.0m, false, 1L),
            (project.TotalFunding, project.ActualCost, project.RemainingFunding, project.UsedPercent, project.IsOverspent, project.FundingVersion));
        Assert.Equal(
            [(Caller, 4m, 200m), (colleague, 6m, 300m)],
            (await RowsAsync<MemberCostItemView>()).OrderBy(c => c.Cost).Select(c => (c.EmployeeId, c.Hours, c.Cost)));
        Assert.Equal(
            [("Auth", 200m), ("Billing", 300m)],
            (await RowsAsync<WorkPackageCostItemView>()).OrderBy(c => c.Cost).Select(c => (c.Description, c.Cost)));
        Assert.Equal(
            [("Budget", 800m), ("", 200m)],
            (await RowsAsync<FundingTrancheItemView>()).OrderBy(t => t.Date).Select(t => (t.Reason, t.Amount)));
    }

    [Fact]
    public async Task Repeated_delivery_does_not_double_any_cost_or_tranche()
    {
        await CreateProjectAsync();
        var auth = await CreateWorkPackageAsync("Auth");
        var login = await CreateTaskAsync(auth, "Login");
        await SendAndDeliverAsync(new StartTaskCommand(auth, login));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 100m, Today, "Budget"));
        await SendAndDeliverAsync(new LogTimeCommand(auth, login, Today, 3m));

        await RedeliverAllEventsAsync();

        Assert.Single(await RowsAsync<TimeEntryItemView>());
        Assert.Single(await RowsAsync<FundingTrancheItemView>());
        var project = Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Equal((100m, 150m, -50m, true), (project.TotalFunding, project.ActualCost, project.RemainingFunding, project.IsOverspent));
    }
}
