using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Ends team memberships. An employee who is responsible for an open work package or task cannot
/// leave the team. Work packages may change concurrently, so the same rule is re-checked after
/// the commit by a handler of <c>TeamMembershipEnded</c>. Only <see cref="Project"/> is changed.
/// </summary>
public sealed class TeamMembershipService
{
    /// <param name="workPackages">
    /// Work packages of the project in which the employee may be responsible for open work.
    /// The caller finds them with a query; the rule itself is checked here on the loaded aggregates.
    /// </param>
    public void EndTeamMembership(
        Project project,
        IEnumerable<WorkPackage> workPackages,
        Guid employeeId,
        DateOnly to)
    {
        var isResponsible = new IsResponsibleForOpenWorkSpecification(employeeId);

        if (workPackages.Any(wp => wp.ProjectId == project.Id && isResponsible.IsSatisfiedBy(wp)))
            throw new DomainException(
                $"Employee '{employeeId}' is responsible for an open work package or task and cannot leave the team.");

        project.EndTeamMembership(employeeId, to);
    }
}
