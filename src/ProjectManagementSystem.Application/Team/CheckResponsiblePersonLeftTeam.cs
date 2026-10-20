using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Contracts.Events;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Domain.WorkPackages.Specifications;

namespace ProjectManagementSystem.Application.Team;

/// <summary>
/// Re-checks after the commit the rule that <c>TeamMembershipService</c> checks before it: work could
/// have been assigned to the employee concurrently. Nothing is rolled back; a person is asked to react.
/// </summary>
public sealed class CheckResponsiblePersonLeftTeam(
    IWorkPackageRepository workPackages,
    IApplicationEventPublisher publisher,
    IUnitOfWork unitOfWork) : IDomainEventHandler<TeamMembershipEnded>
{
    public async Task HandleAsync(TeamMembershipEnded domainEvent, CancellationToken cancellationToken = default)
    {
        var isResponsible = new IsResponsibleForOpenWorkSpecification(domainEvent.EmployeeId);
        var projectWorkPackages = await workPackages.ListByProjectIdAsync(domainEvent.ProjectId, cancellationToken);

        var workPackageIds = projectWorkPackages
            .Where(isResponsible.IsSatisfiedBy)
            .Select(wp => wp.Id)
            .ToList();

        if (workPackageIds.Count == 0)
            return;

        publisher.Publish(new ResponsiblePersonLeftTeam(
            domainEvent.EventId, domainEvent.ProjectId, domainEvent.EmployeeId, workPackageIds));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
