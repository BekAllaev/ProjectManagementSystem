namespace ProjectManagementSystem.Application.Events;

/// <summary>
/// An employee left the project team while still responsible for open work: a work package or a task
/// was assigned to them concurrently with ending the membership. A person must reassign the work manually.
/// <paramref name="CausationEventId"/> is the id of the <c>TeamMembershipEnded</c> event, so a repeated
/// delivery of that event leads to a notification that consumers can recognise as a duplicate.
/// </summary>
public sealed record ResponsiblePersonLeftTeam(
    Guid CausationEventId,
    Guid ProjectId,
    Guid EmployeeId,
    IReadOnlyList<Guid> WorkPackageIds) : ApplicationEvent;
