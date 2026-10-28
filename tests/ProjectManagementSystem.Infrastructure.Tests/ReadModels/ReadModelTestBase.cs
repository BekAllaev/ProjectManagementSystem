using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;

namespace ProjectManagementSystem.Infrastructure.Tests.ReadModels;

/// <summary>
/// Arranges a project through commands, with the caller as a registered employee on its team, and delivers every event,
/// so the read models are filled as in production.
/// </summary>
public abstract class ReadModelTestBase : SqliteTestBase
{
    protected Guid ProjectId { get; private set; }

    protected Guid RoleId { get; private set; }

    /// <summary>
    /// Registered employee who is the caller and a member of the team since 30 days ago, rate 50.
    /// </summary>
    protected Guid Caller { get; private set; }

    protected async Task CreateProjectAsync(string title = "Portal")
    {
        Caller = await RegisterEmployeeAsync("Jonas", "Weber");
        CurrentUser.EmployeeId = Caller;
        ProjectId = (await SendAndDeliverAsync(new CreateProjectCommand(title, "Description", "EUR"))).Id;
        RoleId = (await SendAndDeliverAsync(new CreateRoleCommand(ProjectId, "Developer", null))).Id;
        await AddMemberAsync(Caller);
    }

    protected async Task<Guid> RegisterEmployeeAsync(string firstName, string lastName) =>
        (await SendAndDeliverAsync(new RegisterEmployeeCommand(firstName, lastName, $"{firstName}.{lastName}@example.com".ToLowerInvariant()))).Id;

    protected Task AddMemberAsync(Guid employeeId) =>
        SendAndDeliverAsync(new AddTeamMemberCommand(ProjectId, employeeId, RoleId, Today.AddDays(-30), 50m));

    protected async Task<Guid> CreateWorkPackageAsync(string description, Guid? responsiblePersonId = null) =>
        (await SendAndDeliverAsync(new CreateWorkPackageCommand(ProjectId, description, responsiblePersonId ?? Caller))).Id;

    protected async Task<Guid> CreateTaskAsync(Guid workPackageId, string name, Guid? responsiblePersonId = null, decimal effort = 8m) =>
        (await SendAndDeliverAsync(new CreateTaskCommand(workPackageId, name, $"About {name}", responsiblePersonId ?? Caller, effort))).Id;
}
