using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.WorkPackages.Events;

namespace ProjectManagementSystem.Application.Projects.EventHandlers;

/// <summary>
/// The side of <see cref="ResponsiblePersonCheck"/> that reacts to assignments; leaving the team is handled by
/// <see cref="ProjectEventHandler"/>.
/// </summary>
public sealed class CheckResponsiblePersonsAreTeamMembers(ResponsiblePersonCheck check) :
    IDomainEventHandler<WorkPackageCreated>,
    IDomainEventHandler<ResponsiblePersonChanged>,
    IDomainEventHandler<TaskAdded>
{
    public Task HandleAsync(WorkPackageCreated domainEvent, CancellationToken cancellationToken = default) =>
        check.CheckAssignmentAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);

    public Task HandleAsync(ResponsiblePersonChanged domainEvent, CancellationToken cancellationToken = default) =>
        check.CheckAssignmentAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);

    public Task HandleAsync(TaskAdded domainEvent, CancellationToken cancellationToken = default) =>
        check.CheckAssignmentAsync(domainEvent.ProjectId, domainEvent.WorkPackageId, domainEvent.ResponsiblePersonId, cancellationToken);
}
