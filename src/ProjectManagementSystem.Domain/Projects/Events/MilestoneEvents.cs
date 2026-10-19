using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record MilestoneCreated(
    Guid ProjectId,
    Guid MilestoneId,
    string Name,
    string Description,
    DateOnly TargetDate) : IDomainEvent;

public sealed record MilestoneTaskAdded(Guid ProjectId, Guid MilestoneId, Guid TaskId) : IDomainEvent;

public sealed record MilestoneTaskRemoved(Guid ProjectId, Guid MilestoneId, Guid TaskId) : IDomainEvent;

public sealed record MilestoneTargetDateChanged(Guid ProjectId, Guid MilestoneId, DateOnly TargetDate) : IDomainEvent;

public sealed record MilestoneRenamed(Guid ProjectId, Guid MilestoneId, string Name) : IDomainEvent;

public sealed record MilestoneCompleted(Guid ProjectId, Guid MilestoneId, DateTime CompletedAt) : IDomainEvent;

public sealed record MilestoneDeleted(Guid ProjectId, Guid MilestoneId) : IDomainEvent;
