using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;
using ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Infrastructure.DemoData;

namespace ProjectManagementSystem.Infrastructure.Tests;

/// <summary>
/// The demo data goes through every command it uses without breaking a rule, and fills the read models.
/// </summary>
public sealed class DemoDataSeederTests : SqliteTestBase
{
    [Fact]
    public async Task Seeds_the_projects_of_the_prototype_once()
    {
        Assert.True(await SeedAsync());

        var projects = await SendAsync(new GetProjectsQuery(null, null));
        Assert.Equal(
            [("Billing Service v2", ProjectState.InProgress), ("Customer Portal Relaunch", ProjectState.InProgress),
             ("ERP Data Migration", ProjectState.Created), ("Internal Wiki", ProjectState.Completed),
             ("Warehouse Scanner App", ProjectState.InProgress)],
            projects.Items.Select(p => (p.Title, p.State)));
        Assert.True(projects.Items.Single(p => p.Title == "Warehouse Scanner App").IsOverspent);
        Assert.Equal(8, (await SendAsync(new GetEmployeesQuery(null))).Items.Count);

        var portal = projects.Items.Single(p => p.Title == "Customer Portal Relaunch").Id;
        var milestones = await SendAsync(new GetMilestonesQuery(portal));
        Assert.Contains(milestones.Items, m => m.Health == MilestoneHealth.Late);
        Assert.Contains(milestones.Items, m => m.Health == MilestoneHealth.Reached);
        Assert.Contains((await SendAsync(new GetDependenciesQuery(portal))).Items, d => d.HasWarning);

        // Jonas is responsible for work in progress and was told that a predecessor of the pentest was cancelled.
        CurrentUser.EmployeeId = (await SendAsync(new GetEmployeesQuery("jonas"))).Items.Single().Id;
        Assert.NotEmpty((await SendAsync(new GetDashboardQuery())).OpenTasks);
        Assert.NotEmpty((await SendAsync(new GetNotificationsQuery(UnreadOnly: true, Take: null))).Items);

        Assert.False(await SeedAsync());
    }

    private async Task<bool> SeedAsync()
    {
        var seeded = false;
        await InScopeAsync(async services =>
            seeded = await ActivatorUtilities.CreateInstance<DemoDataSeeder>(services).SeedAsync(id => CurrentUser.EmployeeId = id));

        return seeded;
    }
}
