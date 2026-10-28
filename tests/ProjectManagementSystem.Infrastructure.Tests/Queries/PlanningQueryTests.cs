using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Application.Risks.Queries.GetRisks;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Milestones;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

/// <summary>
/// Dependencies, milestones, funding and risks of one project.
/// </summary>
public sealed class PlanningQueryTests : SqliteTestBase
{
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Project _project;
    private readonly WorkPackage _first;
    private readonly WorkPackage _second;
    private readonly ProjectTask _design;
    private readonly ProjectTask _build;
    private readonly ProjectTask _test;
    private readonly ProjectTask _dropped;

    public PlanningQueryTests()
    {
        _project = TestData.ProjectWithTeam(Me, _colleague);
        _first = TestData.WorkPackage(_project, Me);
        _second = TestData.WorkPackage(_project, Me);
        _design = AddTask(_project, _first, "Design", Me);
        _build = AddTask(_project, _first, "Build", Me);
        _test = AddTask(_project, _second, "Test", _colleague);
        _dropped = AddTask(_project, _second, "Dropped", Me);
        _first.StartTask(_design.Id, Me);
        _first.CompleteTask(_design.Id, Me);
        _first.StartTask(_build.Id, Me);
        _second.CancelTask(_dropped.Id, Me);
    }

    [Fact]
    public async Task Dependencies_show_both_tasks_the_package_crossing_and_the_warning()
    {
        var network = new ScheduleNetwork(_project.Id);
        var schedule = new ScheduleNetworkService();
        schedule.AddDependency(_project, network, _first, _design.Id, _first, _build.Id, DependencyType.FinishToStart);
        schedule.AddDependency(_project, network, _first, _build.Id, _second, _test.Id, DependencyType.FinishToStart);
        await StoreAsync(_project, _first, _second, network);

        var dependencies = await SendAsync(new GetDependenciesQuery(_project.Id));

        Assert.Equal([("Build", "Test"), ("Design", "Build")], dependencies.Items.Select(d => (d.Predecessor.Name, d.Successor.Name)));
        Assert.Equal([true, false], dependencies.Items.Select(d => d.IsAcrossWorkPackages));
        Assert.Equal([true, false], dependencies.Items.Select(d => d.HasWarning));
        Assert.Equal(ProjectTaskStatus.InProgress, dependencies.Items[0].Predecessor.Status);
    }

    [Fact]
    public async Task Project_without_a_network_has_no_dependencies()
    {
        await StoreAsync(_project);

        var dependencies = await SendAsync(new GetDependenciesQuery(_project.Id));

        Assert.Empty(dependencies.Items);
        Assert.Equal(0, dependencies.Version);
    }

    [Fact]
    public async Task Milestones_are_ordered_by_target_date_and_tell_their_health()
    {
        var milestones = new MilestoneService();
        var late = milestones.Create(_project, "Late", "Past its target", Today.AddDays(-3));
        milestones.AddTask(_project, late, _first, _build.Id);
        var looseEnds = milestones.Create(_project, "Loose ends", "", Today.AddDays(-10));
        milestones.AddTask(_project, looseEnds, _first, _design.Id);
        milestones.AddTask(_project, looseEnds, _first, _build.Id);
        looseEnds.Complete(TestData.Now);
        var reached = milestones.Create(_project, "Reached", "", Today.AddDays(-20));
        milestones.AddTask(_project, reached, _first, _design.Id);
        reached.Complete(TestData.Now);
        var empty = milestones.Create(_project, "Empty", "", Today.AddDays(-30));
        empty.Complete(TestData.Now);
        var open = milestones.Create(_project, "Open", "", Today.AddDays(10));
        milestones.AddTask(_project, open, _first, _build.Id);
        milestones.AddTask(_project, open, _second, _test.Id);
        await StoreAsync(_project, _first, _second, late, looseEnds, reached, empty, open);
        // Cancelled after it was added: it stays in the milestone but is not counted.
        await CancelTestAsync();

        var list = await SendAsync(new GetMilestonesQuery(_project.Id));

        Assert.Equal(["Empty", "Reached", "Loose ends", "Late", "Open"], list.Items.Select(m => m.Name));
        Assert.Equal(
            [MilestoneHealth.CompletedEmpty, MilestoneHealth.Reached, MilestoneHealth.CompletedWithLooseEnds, MilestoneHealth.Late, MilestoneHealth.Open],
            list.Items.Select(m => m.Health));
        Assert.Equal([0, 0, 0, 3, 0], list.Items.Select(m => m.DaysLate));
        var looseItem = list.Items[2];
        Assert.Equal((1, 2, 50), (looseItem.CompletedTasks, looseItem.CountedTasks, looseItem.ProgressPercent));
        Assert.Equal(MilestoneStatus.Completed, looseItem.Status);
        var openItem = list.Items[4];
        Assert.Equal([("Build", true), ("Test", false)], openItem.Tasks.Select(t => (t.Name, t.IsCounted)));
        Assert.Equal((0, 1), (openItem.CompletedTasks, openItem.CountedTasks));
    }

    [Fact]
    public async Task Funding_sums_the_tranches_against_the_cost_of_the_logged_time()
    {
        var fundingService = new FundingService();
        var funding = fundingService.CreateFunding(_project);
        fundingService.AddFunding(_project, funding, 800m, Today.AddDays(-20), "Initial budget");
        fundingService.AddFunding(_project, funding, 200m, Today.AddDays(-5), "Extension");
        var timeLogs = new TaskTimeLogService(TestData.TimeProvider);
        var buildLog = timeLogs.CreateTimeLog(_first, _build.Id);
        timeLogs.LogTime(_project, _first, buildLog, Me, Today, 4m);
        timeLogs.LogTime(_project, _first, buildLog, _colleague, Today, 1m);
        _second.StartTask(_test.Id, Me);
        var testLog = timeLogs.CreateTimeLog(_second, _test.Id);
        timeLogs.LogTime(_project, _second, testLog, _colleague, Today, 6m);
        await StoreAsync(_project, _first, _second, funding, buildLog, testLog);

        var summary = await SendAsync(new GetFundingQuery(_project.Id));

        // Every member has the rate 50: 4 h + 1 h on Build, 6 h on Test.
        Assert.Equal((1000m, 550m, 450m, 55.0m, false), (summary.TotalFunding, summary.ActualCost, summary.RemainingFunding, summary.UsedPercent, summary.IsOverspent));
        Assert.Equal(["Extension", "Initial budget"], summary.Tranches.Select(t => t.Reason));
        Assert.Equal([(_colleague, 7m, 350m), (Me, 4m, 200m)], summary.CostByMember.Select(c => (c.Employee.Id, c.Hours, c.Cost)));
        Assert.Equal([(_second.Id, 300m), (_first.Id, 250m)], summary.CostByWorkPackage.Select(c => (c.WorkPackageId, c.Cost)));
        Assert.Equal("USD", summary.Currency);
    }

    [Fact]
    public async Task Project_without_funding_has_no_used_percent()
    {
        await StoreAsync(_project);

        var summary = await SendAsync(new GetFundingQuery(_project.Id));

        Assert.Equal((0m, 0m, (decimal?)null, false, 0L), (summary.TotalFunding, summary.ActualCost, summary.UsedPercent, summary.IsOverspent, summary.Version));
        Assert.Empty(summary.Tranches);
    }

    [Fact]
    public async Task Risks_come_open_first_by_score_with_their_level()
    {
        var owner = await EmployeeAsync("Jonas", "Weber");
        var outage = new Risk(Guid.NewGuid(), _project.Id, "IdP outage", RiskLevel.High, RiskLevel.High, owner.Id, RiskStatus.Open);
        var capacity = new Risk(Guid.NewGuid(), _project.Id, "Capacity", RiskLevel.Medium, RiskLevel.High, owner.Id, RiskStatus.Open);
        var browsers = new Risk(Guid.NewGuid(), _project.Id, "Browsers", RiskLevel.Low, RiskLevel.Medium, owner.Id, RiskStatus.Open);
        var api = new Risk(Guid.NewGuid(), _project.Id, "API change", RiskLevel.High, RiskLevel.High, owner.Id, RiskStatus.Closed);
        var elsewhere = new Risk(Guid.NewGuid(), Guid.NewGuid(), "Other project", RiskLevel.High, RiskLevel.High, owner.Id, RiskStatus.Open);
        await StoreAsync(owner, _project, outage, capacity, browsers, api, elsewhere);

        var all = await SendAsync(new GetRisksQuery(_project.Id, null));
        var closed = await SendAsync(new GetRisksQuery(_project.Id, RiskStatus.Closed));

        Assert.Equal(["IdP outage", "Capacity", "Browsers", "API change"], all.Items.Select(r => r.Description));
        Assert.Equal([9, 6, 2, 9], all.Items.Select(r => r.Score));
        Assert.Equal([RiskLevel.High, RiskLevel.High, RiskLevel.Low, RiskLevel.High], all.Items.Select(r => r.Level));
        Assert.Equal("Weber", all.Items[0].Owner.LastName);
        Assert.Equal(["API change"], closed.Items.Select(r => r.Description));
    }

    private Task CancelTestAsync() =>
        InScopeAsync(async services =>
        {
            var workPackages = services.GetRequiredService<IRepository<WorkPackage>>();
            var stored = (await workPackages.FindAsync(_second.Id))!;
            stored.CancelTask(_test.Id, Me);
            workPackages.Update(stored);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
}
