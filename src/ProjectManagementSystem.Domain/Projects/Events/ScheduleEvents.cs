using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Projects.Schedule;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record DependencyAdded(Guid ProjectId, Dependency Dependency) : DomainEvent;

public sealed record DependencyRemoved(Guid ProjectId, Dependency Dependency) : DomainEvent;

/// <summary>
/// Predecessor of <see cref="Dependency.SuccessorTaskId"/> was cancelled and the dependency was removed.
/// Used to notify the responsible person of the successor task.
/// </summary>
public sealed record PredecessorCancelled(Guid ProjectId, Dependency Dependency) : DomainEvent;
