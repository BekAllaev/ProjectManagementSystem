using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.WorkPackages.Events;

public sealed record WorkPackageCreated(Guid ProjectId, Guid WorkPackageId, string Description, Guid ResponsiblePersonId) : DomainEvent;

/// <summary>
/// Responsible person of the work package (<paramref name="TaskId"/> is empty) or of one of its tasks was changed.
/// </summary>
public sealed record ResponsiblePersonChanged(Guid ProjectId, Guid WorkPackageId, Guid? TaskId, Guid ResponsiblePersonId) : DomainEvent;

public sealed record WorkPackageCompleted(Guid ProjectId, Guid WorkPackageId) : DomainEvent;

public sealed record WorkPackageCancelled(
    Guid ProjectId,
    Guid WorkPackageId,
    string Reason,
    IReadOnlyList<Guid> CancelledTaskIds) : DomainEvent;

public sealed record TaskAdded(
    Guid ProjectId,
    Guid WorkPackageId,
    Guid TaskId,
    string Name,
    string Description,
    Guid ResponsiblePersonId,
    decimal EstimatedEffort) : DomainEvent;

public sealed record TaskStarted(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : DomainEvent;

public sealed record TaskCompleted(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : DomainEvent;

public sealed record TaskCancelled(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : DomainEvent;
