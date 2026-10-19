using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects.Schedule;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record DependencyAdded(Guid ProjectId, Dependency Dependency) : IDomainEvent;

public sealed record DependencyRemoved(Guid ProjectId, Dependency Dependency) : IDomainEvent;

/// <summary>
/// Predecessor of <see cref="Dependency.SuccessorTaskId"/> was cancelled and the dependency was removed.
/// Used to notify the responsible person of the successor task.
/// </summary>
public sealed record PredecessorCancelled(Guid ProjectId, Dependency Dependency) : IDomainEvent;
