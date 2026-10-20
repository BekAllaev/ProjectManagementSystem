using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Milestones.Events;

public sealed record MilestoneCreated(
    Guid ProjectId,
    Guid MilestoneId,
    string Name,
    string Description,
    DateOnly TargetDate) : DomainEvent;

public sealed record MilestoneTaskAdded(Guid ProjectId, Guid MilestoneId, Guid TaskId) : DomainEvent;

public sealed record MilestoneTaskRemoved(Guid ProjectId, Guid MilestoneId, Guid TaskId) : DomainEvent;

public sealed record MilestoneTargetDateChanged(Guid ProjectId, Guid MilestoneId, DateOnly TargetDate) : DomainEvent;

public sealed record MilestoneRenamed(Guid ProjectId, Guid MilestoneId, string Name) : DomainEvent;

public sealed record MilestoneCompleted(Guid ProjectId, Guid MilestoneId, DateTime CompletedAt) : DomainEvent;

public sealed record MilestoneDeleted(Guid ProjectId, Guid MilestoneId) : DomainEvent;
