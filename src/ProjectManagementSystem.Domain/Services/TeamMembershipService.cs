using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Ends team memberships. An employee who is responsible for an open work package or task cannot
/// leave the team. Work packages may change concurrently, so the same rule is re-checked after
/// the commit by a handler of <c>TeamMembershipEnded</c>.
/// </summary>
public sealed class TeamMembershipService(IWorkPackageRepository workPackages)
{
    public async Task EndTeamMembershipAsync(
        Project project,
        Guid employeeId,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var isResponsible = new IsResponsibleForOpenWorkSpecification(employeeId);
        var projectWorkPackages = await workPackages.ListByProjectIdAsync(project.Id, cancellationToken);

        if (projectWorkPackages.Any(isResponsible.IsSatisfiedBy))
            throw new DomainException(
                $"Employee '{employeeId}' is responsible for an open work package or task and cannot leave the team.");

        project.EndTeamMembership(employeeId, to);
    }
}
