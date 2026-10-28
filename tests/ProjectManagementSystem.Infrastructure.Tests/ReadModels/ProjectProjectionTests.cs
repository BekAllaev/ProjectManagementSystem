using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.DeleteRole;
using ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.RenameRole;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

/// <summary>
/// Commands go through the pipeline, their events through the outbox to the projections.
/// </summary>
public sealed class ProjectProjectionTests : SqliteTestBase
{
    [Fact]
    public async Task Row_of_the_project_follows_its_events_with_the_version()
    {
        var projectId = (await SendAndDeliverAsync(new CreateProjectCommand("Portal", "Self-service portal", "EUR"))).Id;

        var created = Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Equal(("Portal", "Self-service portal", "EUR", ProjectState.Created, true, 0L),
            (created.Title, created.Description, created.Currency, created.State, created.IsOpen, created.Version));

        await SendAndDeliverAsync(new StartProjectCommand(projectId));

        var started = Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Equal((ProjectState.InProgress, Clock.GetUtcNow().UtcDateTime, 1L), (started.State, started.StartDate, started.Version));
    }

    [Fact]
    public async Task Roles_and_team_follow_their_events()
    {
        var employeeId = (await SendAndDeliverAsync(new RegisterEmployeeCommand("Sofia", "Rossi", "sofia@example.com"))).Id;
        var projectId = (await SendAndDeliverAsync(new CreateProjectCommand("Portal", null, "EUR"))).Id;
        var analyst = (await SendAndDeliverAsync(new CreateRoleCommand(projectId, "Analyst", "Writes the requirements"))).Id;
        var developer = (await SendAndDeliverAsync(new CreateRoleCommand(projectId, "Developer", null))).Id;
        var tester = (await SendAndDeliverAsync(new CreateRoleCommand(projectId, "Tester", null))).Id;
        var unused = (await SendAndDeliverAsync(new CreateRoleCommand(projectId, "Unused", null))).Id;
        await SendAndDeliverAsync(new AddTeamMemberCommand(projectId, employeeId, analyst, Today.AddDays(-60), 58m, Today.AddDays(-40)));
        await SendAndDeliverAsync(new AddTeamMemberCommand(projectId, employeeId, developer, Today.AddDays(-20), 60m));
        await SendAndDeliverAsync(new ChangeHourlyRateCommand(projectId, employeeId, 65m, Today.AddDays(5)));
        await SendAndDeliverAsync(new RenameRoleCommand(projectId, tester, "QA"));
        await SendAndDeliverAsync(new DeleteRoleCommand(projectId, unused));
        await SendAndDeliverAsync(new RemoveTeamMemberCommand(projectId, employeeId, Today.AddDays(30)));

        var member = Assert.Single(await RowsAsync<TeamMemberItemView>());
        Assert.Equal((developer, "Developer"), (member.RoleId, member.RoleName));
        Assert.Equal(
            [(Today.AddDays(-60), Today.AddDays(-40)), (Today.AddDays(-20), Today.AddDays(30))],
            (await RowsAsync<MembershipPeriodView>()).OrderBy(p => p.From).Select(p => (p.From, p.To)));
        Assert.Equal([58m, 60m, 65m], (await RowsAsync<HourlyRateItemView>()).OrderBy(r => r.From).Select(r => r.Amount));
        Assert.Equal(
            [("Analyst", true), ("Developer", true), ("QA", false)],
            (await RowsAsync<RoleItemView>()).OrderBy(r => r.Name).Select(r => (r.Name, r.IsAssigned)));
        Assert.Equal("Writes the requirements", (await RowsAsync<RoleItemView>()).Single(r => r.Id == analyst).Description);
        // Created with version 0, then one saved change per command: 4 roles, 2 members, a rate, a rename, a delete, the end.
        Assert.Equal(10, Assert.Single(await RowsAsync<ProjectItemView>()).Version);
    }

    [Fact]
    public async Task Repeated_delivery_changes_nothing()
    {
        var employeeId = (await SendAndDeliverAsync(new RegisterEmployeeCommand("Sofia", "Rossi", "sofia@example.com"))).Id;
        var projectId = (await SendAndDeliverAsync(new CreateProjectCommand("Portal", null, "EUR"))).Id;
        var role = (await SendAndDeliverAsync(new CreateRoleCommand(projectId, "Developer", null))).Id;
        await SendAndDeliverAsync(new AddTeamMemberCommand(projectId, employeeId, role, Today, 60m));

        await RedeliverAllEventsAsync();

        Assert.Single(await RowsAsync<ProjectItemView>());
        Assert.Single(await RowsAsync<RoleItemView>());
        Assert.Single(await RowsAsync<TeamMemberItemView>());
        Assert.Single(await RowsAsync<MembershipPeriodView>());
        Assert.Single(await RowsAsync<HourlyRateItemView>());
        Assert.Equal(2, Assert.Single(await RowsAsync<ProjectItemView>()).Version);
    }
}
