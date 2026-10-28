using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;
using ProjectManagementSystem.Application.Risks.Queries.GetRisks;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

/// <summary>
/// Dependencies, milestones, funding and risks of one project.
/// </summary>
public sealed class PlanningQueryTests : ReadModelTestBase
{
    private Guid _colleague;
    private Guid _first;
    private Guid _second;
    private Guid _design;
    private Guid _build;
    private Guid _test;

    [Fact]
    public async Task Dependencies_show_both_tasks_the_package_crossing_and_the_warning()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _first, _design, _first, _build, DependencyType.FinishToStart));
        await SendAndDeliverAsync(new AddDependencyCommand(ProjectId, _first, _build, _second, _test, DependencyType.FinishToStart));

        var dependencies = await SendAsync(new GetDependenciesQuery(ProjectId));

        Assert.Equal([("Build", "Test"), ("Design", "Build")], dependencies.Items.Select(d => (d.Predecessor.Name, d.Successor.Name)));
        Assert.Equal([true, false], dependencies.Items.Select(d => d.IsAcrossWorkPackages));
        Assert.Equal([true, false], dependencies.Items.Select(d => d.HasWarning));
        Assert.Equal(ProjectTaskStatus.InProgress, dependencies.Items[0].Predecessor.Status);
        Assert.Equal(1, dependencies.Version);
    }

    [Fact]
    public async Task Project_without_a_network_has_no_dependencies()
    {
        await CreateProjectAsync();

        var dependencies = await SendAsync(new GetDependenciesQuery(ProjectId));

        Assert.Empty(dependencies.Items);
        Assert.Equal(0, dependencies.Version);
    }

    [Fact]
    public async Task Milestones_are_ordered_by_target_date_and_tell_their_health()
    {
        await ArrangeAsync();
        await MilestoneAsync("Late", Today.AddDays(-3), _build);
        var looseEnds = await MilestoneAsync("Loose ends", Today.AddDays(-10), _design, _build);
        var reached = await MilestoneAsync("Reached", Today.AddDays(-20), _design);
        var empty = await MilestoneAsync("Empty", Today.AddDays(-30));
        await MilestoneAsync("Open", Today.AddDays(10), _build, _test);
        foreach (var completed in new[] { looseEnds, reached, empty })
            await SendAndDeliverAsync(new CompleteMilestoneCommand(completed));
        // Cancelled after it was added: it stays in the milestone but is not counted.
        await SendAndDeliverAsync(new CancelTaskCommand(_second, _test));

        var list = await SendAsync(new GetMilestonesQuery(ProjectId));

        Assert.Equal(["Empty", "Reached", "Loose ends", "Late", "Open"], list.Items.Select(m => m.Name));
        Assert.Equal(
            [MilestoneHealth.CompletedEmpty, MilestoneHealth.Reached, MilestoneHealth.CompletedWithLooseEnds, MilestoneHealth.Late, MilestoneHealth.Open],
            list.Items.Select(m => m.Health));
        Assert.Equal([0, 0, 0, 3, 0], list.Items.Select(m => m.DaysLate));
        var looseItem = list.Items[2];
        Assert.Equal((1, 2, 50, MilestoneStatus.Completed), (looseItem.CompletedTasks, looseItem.CountedTasks, looseItem.ProgressPercent, looseItem.Status));
        var openItem = list.Items[4];
        Assert.Equal([("Build", true), ("Test", false)], openItem.Tasks.Select(t => (t.Name, t.IsCounted)));
        Assert.Equal((0, 1), (openItem.CompletedTasks, openItem.CountedTasks));
    }

    [Fact]
    public async Task Funding_shows_the_tranches_against_the_cost_of_the_logged_time()
    {
        await ArrangeAsync();
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 800m, Today.AddDays(-20), "Initial budget"));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 200m, Today.AddDays(-5), "Extension"));
        await SendAndDeliverAsync(new StartTaskCommand(_second, _test));
        await SendAndDeliverAsync(new LogTimeCommand(_first, _build, Today, 4m));
        CurrentUser.EmployeeId = _colleague;
        await SendAndDeliverAsync(new LogTimeCommand(_first, _build, Today, 1m));
        await SendAndDeliverAsync(new LogTimeCommand(_second, _test, Today, 6m));
        CurrentUser.EmployeeId = Caller;

        var summary = await SendAsync(new GetFundingQuery(ProjectId));

        // Every member has the rate 50: 4 h + 1 h on Build, 6 h on Test.
        Assert.Equal((1000m, 550m, 450m, 55.0m, false), (summary.TotalFunding, summary.ActualCost, summary.RemainingFunding, summary.UsedPercent, summary.IsOverspent));
        Assert.Equal(["Extension", "Initial budget"], summary.Tranches.Select(t => t.Reason));
        Assert.Equal([("Tanaka", 7m, 350m), ("Weber", 4m, 200m)], summary.CostByMember.Select(c => (c.Employee.LastName, c.Hours, c.Cost)));
        Assert.Equal([("Second", 300m), ("First", 250m)], summary.CostByWorkPackage.Select(c => (c.Description, c.Cost)));
        Assert.Equal(("EUR", 1L), (summary.Currency, summary.Version));
    }

    [Fact]
    public async Task Project_without_funding_has_no_used_percent()
    {
        await CreateProjectAsync();

        var summary = await SendAsync(new GetFundingQuery(ProjectId));

        Assert.Equal((0m, 0m, (decimal?)null, false, 0L), (summary.TotalFunding, summary.ActualCost, summary.UsedPercent, summary.IsOverspent, summary.Version));
        Assert.Empty(summary.Tranches);
    }

    [Fact]
    public async Task Risks_come_open_first_by_score_with_their_level()
    {
        await CreateProjectAsync();
        await RiskAsync("IdP outage", RiskLevel.High, RiskLevel.High);
        await RiskAsync("Capacity", RiskLevel.Medium, RiskLevel.High);
        await RiskAsync("Browsers", RiskLevel.Low, RiskLevel.Medium);
        var api = await RiskAsync("API change", RiskLevel.High, RiskLevel.High);
        await SendAndDeliverAsync(new UpdateRiskCommand(api, "API change", RiskLevel.High, RiskLevel.High, Caller, RiskStatus.Closed));

        var all = await SendAsync(new GetRisksQuery(ProjectId, null));
        var closed = await SendAsync(new GetRisksQuery(ProjectId, RiskStatus.Closed));

        Assert.Equal(["IdP outage", "Capacity", "Browsers", "API change"], all.Items.Select(r => r.Description));
        Assert.Equal([9, 6, 2, 9], all.Items.Select(r => r.Score));
        Assert.Equal([RiskLevel.High, RiskLevel.High, RiskLevel.Low, RiskLevel.High], all.Items.Select(r => r.Level));
        Assert.Equal("Weber", all.Items[0].Owner.LastName);
        Assert.Equal(["API change"], closed.Items.Select(r => r.Description));
    }

    private async Task ArrangeAsync()
    {
        await CreateProjectAsync();
        _colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(_colleague);
        _first = await CreateWorkPackageAsync("First");
        _second = await CreateWorkPackageAsync("Second");
        _design = await CreateTaskAsync(_first, "Design");
        _build = await CreateTaskAsync(_first, "Build");
        _test = await CreateTaskAsync(_second, "Test", _colleague);
        await SendAndDeliverAsync(new StartTaskCommand(_first, _design));
        await SendAndDeliverAsync(new CompleteTaskCommand(_first, _design));
        await SendAndDeliverAsync(new StartTaskCommand(_first, _build));
    }

    private async Task<Guid> MilestoneAsync(string name, DateOnly targetDate, params Guid[] tasks)
    {
        var id = (await SendAndDeliverAsync(new CreateMilestoneCommand(ProjectId, name, null, targetDate))).Id;
        foreach (var task in tasks)
            await SendAndDeliverAsync(new AddTaskToMilestoneCommand(id, task == _test ? _second : _first, task));

        return id;
    }

    private async Task<Guid> RiskAsync(string description, RiskLevel probability, RiskLevel impact) =>
        (await SendAndDeliverAsync(new RegisterRiskCommand(ProjectId, description, probability, impact, Caller))).Id;
}
