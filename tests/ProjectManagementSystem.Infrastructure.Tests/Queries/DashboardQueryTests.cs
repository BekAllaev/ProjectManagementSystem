using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

public sealed class DashboardQueryTests : ReadModelTestBase
{
    [Fact]
    public async Task Dashboard_shows_the_open_tasks_projects_and_funding_alerts_of_the_caller()
    {
        await CreateProjectAsync("Portal");
        var colleague = await RegisterEmployeeAsync("Mei", "Tanaka");
        await AddMemberAsync(colleague);
        var workPackage = await CreateWorkPackageAsync("Auth");
        await CreateTaskAsync(workPackage, "Mine");
        var running = await CreateTaskAsync(workPackage, "Running");
        await CreateTaskAsync(workPackage, "Theirs", colleague);
        var done = await CreateTaskAsync(workPackage, "Done");
        await SendAndDeliverAsync(new StartTaskCommand(workPackage, running));
        await SendAndDeliverAsync(new StartTaskCommand(workPackage, done));
        await SendAndDeliverAsync(new CompleteTaskCommand(workPackage, done));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 100m, Today, "Budget"));
        await SendAndDeliverAsync(new LogTimeCommand(workPackage, running, Today, 1.75m));
        var notMine = (await SendAndDeliverAsync(new CreateProjectCommand("Elsewhere", null, "EUR"))).Id;

        var dashboard = await SendAsync(new GetDashboardQuery());

        Assert.Equal(["Running", "Mine"], dashboard.OpenTasks.Select(t => t.Name));
        Assert.Equal([1.75m, 0m], dashboard.OpenTasks.Select(t => t.LoggedHours));
        Assert.Equal(("Portal", "Auth"), (dashboard.OpenTasks[0].ProjectTitle, dashboard.OpenTasks[0].WorkPackageDescription));
        var project = Assert.Single(dashboard.Projects);
        Assert.Equal((ProjectId, 25), (project.Id, project.ProgressPercent));
        Assert.DoesNotContain(dashboard.Projects, p => p.Id == notMine);
        // 1.75 h at the rate 50 of 100 funding: 87.5% used, above the alert threshold of 80%.
        var alert = Assert.Single(dashboard.FundingAlerts);
        Assert.Equal((ProjectId, 87.5m, 12.5m, 87.5m, false), (alert.ProjectId, alert.ActualCost, alert.RemainingFunding, alert.UsedPercent, alert.IsOverspent));
    }
}
