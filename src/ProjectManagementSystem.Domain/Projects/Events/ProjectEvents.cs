using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Events;

/// <param name="Description">Empty in events saved before it was part of the event.</param>
public sealed record ProjectCreated(Guid ProjectId, string Title, string Description, string Currency, DateTime CreationDate) : DomainEvent;

public sealed record ProjectStarted(Guid ProjectId, DateTime StartDate) : DomainEvent;

public sealed record ProjectCompleted(Guid ProjectId, DateTime CompletionDate) : DomainEvent;

/// <summary>
/// The work package was added to <c>Project.WorkPackageIds</c>, after its creation (see <c>WorkPackageCreated</c>).
/// </summary>
public sealed record WorkPackageRegistered(Guid ProjectId, Guid WorkPackageId) : DomainEvent;
