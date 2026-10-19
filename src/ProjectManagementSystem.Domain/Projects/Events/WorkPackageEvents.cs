using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record WorkPackageCreated(Guid ProjectId, Guid WorkPackageId, string Description, Guid ResponsiblePersonId) : IDomainEvent;

public sealed record WorkPackageResponsiblePersonChanged(Guid ProjectId, Guid WorkPackageId, Guid ResponsiblePersonId) : IDomainEvent;

public sealed record WorkPackageCompleted(Guid ProjectId, Guid WorkPackageId) : IDomainEvent;

public sealed record WorkPackageCancelled(
    Guid ProjectId,
    Guid WorkPackageId,
    string Reason,
    IReadOnlyList<Guid> CancelledTaskIds) : IDomainEvent;

public sealed record TaskAdded(
    Guid ProjectId,
    Guid WorkPackageId,
    Guid TaskId,
    string Name,
    string Description,
    Guid ResponsiblePersonId,
    decimal EstimatedEffort) : IDomainEvent;

public sealed record TaskStarted(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : IDomainEvent;

public sealed record TaskCompleted(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : IDomainEvent;

public sealed record TaskCancelled(Guid ProjectId, Guid WorkPackageId, Guid TaskId) : IDomainEvent;
