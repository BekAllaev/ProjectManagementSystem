using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.TimeTracking;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

public sealed class ProjectQueryTests : QueryTestBase
{
    [Fact]
    public async Task Projects_show_progress_and_funding_and_are_ordered_by_title()
    {
        var portal = new Project(Guid.NewGuid(), "Portal", "Self-service", new Currency("EUR"), TestData.Now);
        var roleId = portal.CreateRole("Developer", "");
        portal.AddTeamMember(Me, roleId, Today.AddDays(-30), hourlyRate: 100m);
        var workPackage = TestData.WorkPackage(portal, Me);
        var done = AddTask(portal, workPackage, "Done", Me);
        var running = AddTask(portal, workPackage, "Running", Me);
        var cancelled = AddTask(portal, workPackage, "Cancelled", Me);
        AddTask(portal, workPackage, "Open", Me);
        workPackage.StartTask(done.Id, Me);
        workPackage.CompleteTask(done.Id, Me);
        workPackage.StartTask(running.Id, Me);
        workPackage.CancelTask(cancelled.Id, Me);

        var fundingService = new FundingService();
        var funding = fundingService.CreateFunding(portal);
        fundingService.AddFunding(portal, funding, 1000m, Today, "Budget");
        var timeLogs = new TaskTimeLogService(TestData.TimeProvider);
        var timeLog = timeLogs.CreateTimeLog(workPackage, running.Id);
        timeLogs.LogTime(portal, workPackage, timeLog, Me, Today, 12m);

        var archive = new Project(Guid.NewGuid(), "Archive", "", new Currency("USD"), TestData.Now);
        await StoreAsync(portal, workPackage, funding, timeLog, archive);

        var list = await SendAsync(new GetProjectsQuery(null, null));

        Assert.Equal(["Archive", "Portal"], list.Items.Select(p => p.Title));
        var item = list.Items[1];
        Assert.Equal((1, 3, 33), (item.CompletedTasks, item.CountedTasks, item.ProgressPercent));
        Assert.Equal((1000m, 1200m, -200m, true), (item.TotalFunding, item.ActualCost, item.RemainingFunding, item.IsOverspent));
        Assert.Equal("EUR", item.Currency);
        Assert.Equal((0, 0, 0m, false), (list.Items[0].CountedTasks, list.Items[0].ProgressPercent, list.Items[0].TotalFunding, list.Items[0].IsOverspent));
    }

    [Fact]
    public async Task Projects_are_filtered_by_state_and_title()
    {
        var created = new Project(Guid.NewGuid(), "Customer Portal", "", new Currency("EUR"), TestData.Now);
        var started = new Project(Guid.NewGuid(), "Billing", "", new Currency("EUR"), TestData.Now);
        started.Start(TestData.Now);
        await StoreAsync(created, started);

        Assert.Equal(["Billing"], (await SendAsync(new GetProjectsQuery(ProjectState.InProgress, null))).Items.Select(p => p.Title));
        Assert.Equal(["Customer Portal"], (await SendAsync(new GetProjectsQuery(null, " portal "))).Items.Select(p => p.Title));
        Assert.Empty((await SendAsync(new GetProjectsQuery(ProjectState.Completed, null))).Items);
    }

    [Fact]
    public async Task Project_counts_the_tasks_of_registered_and_unregistered_work_packages()
    {
        var project = TestData.ProjectWithTeam(Me);
        var registered = TestData.WorkPackage(project, Me);
        AddTask(project, registered, "A", Me);
        var unregistered = WorkPackageService.CreateWorkPackage(project, "Not registered yet", Me);
        var b = AddTask(project, unregistered, "B", Me);
        unregistered.StartTask(b.Id, Me);
        await StoreAsync(project, registered, unregistered);

        var details = await SendAsync(new GetProjectQuery(project.Id));

        Assert.Equal((1, 1, 0, 0), (details.TaskCounts.Open, details.TaskCounts.InProgress, details.TaskCounts.Completed, details.TaskCounts.Cancelled));
        Assert.Equal((2, 0), (details.CountedTasks, details.ProgressPercent));
        Assert.True(details.IsOpen);
        Assert.Equal(ProjectState.Created, details.State);
        Assert.Equal("USD", details.Currency);
    }

    [Fact]
    public async Task Version_of_the_project_grows_with_every_saved_change()
    {
        var project = TestData.ProjectWithTeam(Me);
        await StoreAsync(project);
        Assert.Equal(0, (await SendAsync(new GetProjectQuery(project.Id))).Version);

        await InScopeAsync(async services =>
        {
            var projects = services.GetRequiredService<IRepository<Project>>();
            var stored = (await projects.FindAsync(project.Id))!;
            stored.Start(TestData.Now);
            projects.Update(stored);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        var details = await SendAsync(new GetProjectQuery(project.Id));
        Assert.Equal(1, details.Version);
        Assert.Equal(ProjectState.InProgress, details.State);
    }

    [Fact]
    public async Task Unknown_project_is_not_found()
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
        var amira = await EmployeeAsync("Amira", "Haddad");
        var sofia = await EmployeeAsync("Sofia", "Rossi");
        var lena = await EmployeeAsync("Lena", "Ortiz");
        var project = new Project(Guid.NewGuid(), "Portal", "", new Currency("EUR"), TestData.Now);
        var manager = project.CreateRole("Project Manager", "");
        var analyst = project.CreateRole("Business Analyst", "");
        project.AddTeamMember(amira.Id, manager, Today.AddDays(-60), 85m);
        project.AddTeamMember(sofia.Id, manager, Today.AddDays(-60), 58m, to: Today.AddDays(-40));
        project.AddTeamMember(sofia.Id, analyst, Today.AddDays(-20), 60m);
        project.ChangeHourlyRate(sofia.Id, 65m, Today.AddDays(5), Today);
        project.AddTeamMember(lena.Id, manager, Today.AddDays(-60), 55m, to: Today.AddDays(-1));
        await StoreAsync(amira, sofia, lena, project);

        var team = await SendAsync(new GetTeamQuery(project.Id));

        Assert.Equal(["Haddad", "Rossi", "Ortiz"], team.Members.Select(m => m.Employee.LastName));
        var member = team.Members[1];
        Assert.True(member.IsActive);
        Assert.Equal("Business Analyst", member.RoleName);
        Assert.Equal("sofia.rossi@example.com", member.Email);
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
        var project = new Project(Guid.NewGuid(), "Portal", "", new Currency("EUR"), TestData.Now);
        var developer = project.CreateRole("Developer", "Writes code");
        project.CreateRole("Analyst", "");
        project.AddTeamMember(Me, developer, Today.AddDays(-10), 50m, to: Today.AddDays(-1));
        await StoreAsync(project);

        var roles = await SendAsync(new GetRolesQuery(project.Id));

        Assert.Equal([("Analyst", false), ("Developer", true)], roles.Items.Select(r => (r.Name, r.IsAssigned)));
        Assert.Equal("Writes code", roles.Items[1].Description);
    }
}
