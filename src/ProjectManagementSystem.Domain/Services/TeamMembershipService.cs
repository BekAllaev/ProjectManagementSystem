using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Ends team memberships. An employee who is responsible for an open work package or task cannot
/// leave the team. Only <see cref="Project"/> is changed.
/// </summary>
public sealed class TeamMembershipService
{
    /// <param name="workPackages">
    /// All work packages of the project, loaded by <see cref="Project.WorkPackageIds"/>. An incomplete
    /// list is rejected, so the rule cannot be bypassed by leaving a package out.
    /// </param>
    public void EndTeamMembership(
        Project project,
        IReadOnlyCollection<WorkPackage> workPackages,
        Guid employeeId,
        DateOnly to)
    {
        WorkPackagesOfProject.EnsureComplete(project, workPackages);

        var isResponsible = new IsResponsibleForOpenWorkSpecification(employeeId);

        if (workPackages.Any(wp => wp.ProjectId == project.Id && isResponsible.IsSatisfiedBy(wp)))
            throw new DomainException(
                $"Employee '{employeeId}' is responsible for an open work package or task and cannot leave the team.");

        project.EndTeamMembership(employeeId, to);
    }
}
