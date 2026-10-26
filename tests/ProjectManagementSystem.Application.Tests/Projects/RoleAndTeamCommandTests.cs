using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.DeleteRole;
using ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.RenameRole;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Application.Tests.Projects;

public class RoleAndTeamCommandTests : CommandTestBase
{
    [Fact]
    public async Task CreateRole_NewName_AddsRole()
    {
        var project = SeedProject();

        var roleId = await App.SendAsync(new CreateRoleCommand(project.Id, "Tester", null));

        Assert.Contains(project.Roles, r => r.Id == roleId && r.Name == "Tester");
        AssertCommitted(project, typeof(RoleCreated));
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ThrowsDomainRuleViolationException()
    {
        var project = SeedProject();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new CreateRoleCommand(project.Id, "developer", null)));
    }

    [Fact]
    public async Task RenameRole_NeverAssignedRole_RenamesIt()
    {
        var project = SeedProject();
        var roleId = UnassignedRole(project);

        await App.SendAsync(new RenameRoleCommand(project.Id, roleId, "QA"));

        Assert.Equal("QA", project.Roles.Single(r => r.Id == roleId).Name);
        AssertCommitted(project, typeof(RoleRenamed));
    }

    [Fact]
    public async Task RenameRole_AssignedRole_ThrowsDomainRuleViolationException()
    {
        var project = SeedProject();

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new RenameRoleCommand(project.Id, AssignedRole(project), "Engineer")));
    }

    [Fact]
    public async Task ChangeRoleDescription_ExistingRole_ChangesIt()
    {
        var project = SeedProject();
        var roleId = AssignedRole(project);

        await App.SendAsync(new ChangeRoleDescriptionCommand(project.Id, roleId, "Writes and reviews code"));

        Assert.Equal("Writes and reviews code", project.Roles.Single(r => r.Id == roleId).Description);
        AssertCommitted(project, typeof(RoleDescriptionChanged));
    }

    [Fact]
    public async Task DeleteRole_NeverAssignedRole_DeletesIt()
    {
        var project = SeedProject();
        var roleId = UnassignedRole(project);

        await App.SendAsync(new DeleteRoleCommand(project.Id, roleId));

        Assert.DoesNotContain(project.Roles, r => r.Id == roleId);
        AssertCommitted(project, typeof(RoleDeleted));
    }

    [Fact]
    public async Task DeleteRole_AssignedRole_ThrowsDomainRuleViolationException()
    {
        var project = SeedProject();

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new DeleteRoleCommand(project.Id, AssignedRole(project))));
    }

    [Fact]
    public async Task AddTeamMember_ExistingEmployee_AddsMembershipAndChangesOnlyProject()
    {
        var project = SeedProject();
        var employee = await SeedEmployeeAsync("ada@example.com");

        var teamMemberId = await App.SendAsync(
            new AddTeamMemberCommand(project.Id, employee.Id, AssignedRole(project), Today, HourlyRate: 60m));

        var teamMember = project.TeamMembers.Single(m => m.Id == teamMemberId);
        Assert.Equal(employee.Id, teamMember.EmployeeId);
        Assert.Equal(60m, teamMember.HourlyRates.Single().Amount);
        AssertCommitted(project, typeof(TeamMemberAdded));
    }

    [Fact]
    public async Task AddTeamMember_UnknownEmployee_ThrowsAggregateNotFoundException()
    {
        var project = SeedProject();

        await AssertRejectedAsync<AggregateNotFoundException>(() =>
            App.SendAsync(new AddTeamMemberCommand(project.Id, Guid.NewGuid(), AssignedRole(project), Today, 60m)));
    }

    [Fact]
    public async Task AddTeamMember_OverlappingPeriod_ThrowsDomainRuleViolationException()
    {
        var project = SeedProject();
        var employee = await SeedEmployeeAsync("ada@example.com");
        project.AddTeamMember(employee.Id, AssignedRole(project), Today.AddDays(-10), 50m);
        project.ClearDomainEvents();

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new AddTeamMemberCommand(project.Id, employee.Id, AssignedRole(project), Today, 60m)));
    }

    [Fact]
    public async Task RemoveTeamMember_NotResponsibleForOpenWork_EndsMembership()
    {
        var leaving = Guid.NewGuid();
        var project = SeedProject(leaving);
        SeedWorkPackage(project, taskCount: 1);

        await App.SendAsync(new RemoveTeamMemberCommand(project.Id, leaving, Today));

        Assert.Equal(Today, project.TeamMembers.Single(m => m.EmployeeId == leaving).To);
        AssertCommitted(project, typeof(TeamMembershipEnded));
    }

    [Fact]
    public async Task RemoveTeamMember_ResponsibleForOpenWorkPackage_ThrowsDomainRuleViolationException()
    {
        var project = SeedProject();
        SeedWorkPackage(project);

        await AssertRejectedAsync<DomainRuleViolationException>(() => App.SendAsync(new RemoveTeamMemberCommand(project.Id, Member, Today)));
        Assert.True(project.IsTeamMemberOn(Member, Today.AddDays(1)));
    }

    [Fact]
    public async Task ChangeHourlyRate_FromToday_AddsRate()
    {
        var project = SeedProject();

        await App.SendAsync(new ChangeHourlyRateCommand(project.Id, Member, 70m, Today));

        Assert.Equal(70m, project.GetHourlyRateOn(Member, Today).Amount);
        AssertCommitted(project, typeof(HourlyRateChanged));
    }

    [Fact]
    public async Task ChangeHourlyRate_BeforeTodayOfTimeProvider_ThrowsDomainValidationException()
    {
        var project = SeedProject();
        App.TimeProvider.Advance(TimeSpan.FromDays(10));

        await AssertRejectedAsync<DomainValidationException>(() =>
            App.SendAsync(new ChangeHourlyRateCommand(project.Id, Member, 70m, Today.AddDays(-5))));
    }

    private static Guid AssignedRole(Project project) => project.Roles.Single(r => r.Name == "Developer").Id;

    private static Guid UnassignedRole(Project project)
    {
        var roleId = project.CreateRole("Tester", "Tests");
        project.ClearDomainEvents();

        return roleId;
    }
}
