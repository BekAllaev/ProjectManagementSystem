using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Team;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Application.Team;

public sealed class TeamMembershipAppService(
    IRepository<Project> projects,
    IRepository<WorkPackage> workPackages,
    IWorkPackageLookup workPackageLookup,
    TeamMembershipService teamMembershipService,
    IUnitOfWork unitOfWork) : ITeamMembershipAppService
{
    public async Task EndTeamMembershipAsync(Guid projectId, Guid employeeId, DateOnly to, CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var workPackageIds = await workPackageLookup.ListWorkPackageIdsWithOpenWorkOfAsync(projectId, employeeId, cancellationToken);
        var workPackagesWithOpenWork = await workPackages.GetManyAsync(workPackageIds, cancellationToken);

        teamMembershipService.EndTeamMembership(project, workPackagesWithOpenWork, employeeId, to);

        projects.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
