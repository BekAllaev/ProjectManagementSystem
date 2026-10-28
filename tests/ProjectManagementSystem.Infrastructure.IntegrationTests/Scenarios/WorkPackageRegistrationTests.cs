using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.CloseProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.EventHandlers;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Infrastructure.IntegrationTests.Scenarios;

/// <summary>
/// Eventual consistency of the registration of a work package by <see cref="WorkPackageEventHandler"/> through the real
/// commands, the outbox and the
/// handler: the work package is created in one transaction and registered in its project in the next one.
/// </summary>
public sealed class WorkPackageRegistrationTests(IntegrationTestDatabase database) : IntegrationTestBase(database)
{
    [SqlServerFact]
    public async Task A_created_work_package_is_registered_in_its_project_after_the_commit()
    {
        var (projectId, employeeId) = await StartedProjectWithMemberAsync();

        var workPackageId = (await SendAsync(new CreateWorkPackageCommand(projectId, "Design", employeeId))).Id;
        Assert.DoesNotContain(workPackageId, (await LoadAsync<Project>(projectId)).WorkPackageIds);

        Assert.Null(await DrainOutboxAsync());

        Assert.Contains(workPackageId, (await LoadAsync<Project>(projectId)).WorkPackageIds);
        Assert.Equal(WorkPackageStatus.Open, (await LoadAsync<WorkPackage>(workPackageId)).Status);
    }

    [SqlServerFact]
    public async Task A_work_package_of_a_project_closed_before_the_registration_is_cancelled()
    {
        var (projectId, employeeId) = await StartedProjectWithMemberAsync();
        var workPackageId = (await SendAsync(new CreateWorkPackageCommand(projectId, "Design", employeeId))).Id;

        await SendAsync(new CloseProjectCommand(projectId));
        Assert.Null(await DrainOutboxAsync());

        var workPackage = await LoadAsync<WorkPackage>(workPackageId);
        Assert.Equal(WorkPackageStatus.Cancelled, workPackage.Status);
        Assert.Equal(WorkPackageEventHandler.ProjectClosedReason, workPackage.CancellationReason);
        var project = await LoadAsync<Project>(projectId);
        Assert.Equal(ProjectState.Completed, project.State);
        Assert.DoesNotContain(workPackageId, project.WorkPackageIds);
    }

    private async Task<(Guid ProjectId, Guid EmployeeId)> StartedProjectWithMemberAsync()
    {
        var employeeId = (await SendAsync(new RegisterEmployeeCommand("Ada", "Lovelace", "ada@example.com"))).Id;
        var projectId = (await SendAsync(new CreateProjectCommand("Project", null, "USD"))).Id;
        var roleId = (await SendAsync(new CreateRoleCommand(projectId, "Developer", null))).Id;
        await SendAsync(new AddTeamMemberCommand(projectId, employeeId, roleId, Today, 50m));
        await SendAsync(new StartProjectCommand(projectId));

        return (projectId, employeeId);
    }
}
