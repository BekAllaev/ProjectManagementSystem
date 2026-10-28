using FluentValidation;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

public sealed class ProjectQueryTests : ReadModelTestBase
{
    [Fact]
    public async Task Projects_show_progress_and_funding_and_are_ordered_by_title()
    {
        await CreateProjectAsync("Portal");
        var workPackage = await CreateWorkPackageAsync("Auth");
        var done = await CreateTaskAsync(workPackage, "Done");
        var running = await CreateTaskAsync(workPackage, "Running");
        var cancelled = await CreateTaskAsync(workPackage, "Cancelled");
        await CreateTaskAsync(workPackage, "Open");
        await SendAndDeliverAsync(new StartTaskCommand(workPackage, done));
        await SendAndDeliverAsync(new CompleteTaskCommand(workPackage, done));
        await SendAndDeliverAsync(new StartTaskCommand(workPackage, running));
        await SendAndDeliverAsync(new CancelTaskCommand(workPackage, cancelled));
        await SendAndDeliverAsync(new AddFundingCommand(ProjectId, 500m, Today, "Budget"));
        await SendAndDeliverAsync(new LogTimeCommand(workPackage, running, Today, 12m));
        await SendAndDeliverAsync(new CreateProjectCommand("Archive", null, "USD"));

        var list = await SendAsync(new GetProjectsQuery(null, null));

        Assert.Equal(["Archive", "Portal"], list.Items.Select(p => p.Title));
        var item = list.Items[1];
        Assert.Equal((1, 3, 33), (item.CompletedTasks, item.CountedTasks, item.ProgressPercent));
        // 12 h at the rate 50.
        Assert.Equal((500m, 600m, -100m, true), (item.TotalFunding, item.ActualCost, item.RemainingFunding, item.IsOverspent));
        Assert.Equal("EUR", item.Currency);
        Assert.Equal((0, 0, 0m, false), (list.Items[0].CountedTasks, list.Items[0].ProgressPercent, list.Items[0].TotalFunding, list.Items[0].IsOverspent));
    }

    [Fact]
    public async Task Projects_are_filtered_by_state_and_title()
    {
        await SendAndDeliverAsync(new CreateProjectCommand("Customer Portal", null, "EUR"));
        var billing = (await SendAndDeliverAsync(new CreateProjectCommand("Billing", null, "EUR"))).Id;
        await SendAndDeliverAsync(new StartProjectCommand(billing));

        Assert.Equal(["Billing"], (await SendAsync(new GetProjectsQuery(ProjectState.InProgress, null))).Items.Select(p => p.Title));
        Assert.Equal(["Customer Portal"], (await SendAsync(new GetProjectsQuery(null, " portal "))).Items.Select(p => p.Title));
        Assert.Empty((await SendAsync(new GetProjectsQuery(ProjectState.Completed, null))).Items);
    }

    [Fact]
    public async Task Project_header_counts_the_tasks_and_carries_the_version()
    {
        await CreateProjectAsync();
        var workPackage = await CreateWorkPackageAsync("Auth");
        await CreateTaskAsync(workPackage, "A");
        var b = await CreateTaskAsync(workPackage, "B");
        await SendAndDeliverAsync(new StartTaskCommand(workPackage, b));

        var details = await SendAsync(new GetProjectQuery(ProjectId));

        Assert.Equal((1, 1, 0, 0), (details.TaskCounts.Open, details.TaskCounts.InProgress, details.TaskCounts.Completed, details.TaskCounts.Cancelled));
        Assert.Equal((2, 0), (details.CountedTasks, details.ProgressPercent));
        Assert.Equal((ProjectState.Created, true, "EUR", "Description"), (details.State, details.IsOpen, details.Currency, details.Description));
        // Created at version 0, then the role, the member and the registration of the package.
        Assert.Equal(3, details.Version);

        await SendAndDeliverAsync(new StartProjectCommand(ProjectId));

        Assert.Equal(4, (await SendAsync(new GetProjectQuery(ProjectId))).Version);
    }

    [Fact]
    public async Task Project_without_a_read_model_is_not_found()
    {
        var exception = await Assert.ThrowsAsync<AggregateNotFoundException>(() => SendAsync(new GetProjectQuery(Guid.NewGuid())));

        Assert.Contains(nameof(Project), exception.Message);
    }

    [Fact]
    public async Task Query_with_a_wrong_shape_is_rejected_before_the_handler()
    {
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(new GetProjectQuery(Guid.Empty)));
    }

    [Fact]
    public async Task Team_has_one_item_per_employee_with_every_period_and_rate()
    {
        await CreateProjectAsync();
        var sofia = await RegisterEmployeeAsync("Sofia", "Rossi");
        var lena = await RegisterEmployeeAsync("Lena", "Ortiz");
        var analyst = (await SendAndDeliverAsync(new CreateRoleCommand(ProjectId, "Business Analyst", null))).Id;
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, sofia, RoleId, Today.AddDays(-60), 58m, Today.AddDays(-40)));
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, sofia, analyst, Today.AddDays(-20), 60m));
        await SendAndDeliverAsync(new ChangeHourlyRateCommand(ProjectId, sofia, 65m, Today.AddDays(5)));
        await SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, lena, RoleId, Today.AddDays(-60), 55m));
        await SendAndDeliverAsync(new RemoveTeamMemberCommand(ProjectId, lena, Today.AddDays(-1)));

        var team = await SendAsync(new GetTeamQuery(ProjectId));

        Assert.Equal(["Rossi", "Weber", "Ortiz"], team.Members.Select(m => m.Employee.LastName));
        var member = team.Members[0];
        Assert.True(member.IsActive);
        Assert.Equal(("Business Analyst", "sofia.rossi@example.com"), (member.RoleName, member.Email));
        Assert.Equal([Today.AddDays(-60), Today.AddDays(-20)], member.Periods.Select(p => p.From));
        Assert.Equal([Today.AddDays(-40), null], member.Periods.Select(p => p.To));
        Assert.Equal((60m, Today.AddDays(-20)), (member.CurrentHourlyRate, member.CurrentHourlyRateFrom));
        Assert.Equal([65m, 60m, 58m], member.HourlyRates.Select(r => r.Amount));
        Assert.False(team.Members[2].IsActive);
        Assert.Equal(55m, team.Members[2].CurrentHourlyRate);
    }

    [Fact]
    public async Task Roles_tell_which_were_ever_assigned()
    {
        await CreateProjectAsync();
        await SendAndDeliverAsync(new CreateRoleCommand(ProjectId, "Analyst", "Writes the requirements"));

        var roles = await SendAsync(new GetRolesQuery(ProjectId));

        Assert.Equal([("Analyst", false), ("Developer", true)], roles.Items.Select(r => (r.Name, r.IsAssigned)));
        Assert.Equal("Writes the requirements", roles.Items[0].Description);
    }
}
