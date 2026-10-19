using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record ProjectCreated(Guid ProjectId, string Title, DateTime CreationDate) : IDomainEvent;

public sealed record ProjectStarted(Guid ProjectId, DateTime StartDate) : IDomainEvent;

public sealed record ProjectCompleted(Guid ProjectId, DateTime CompletionDate) : IDomainEvent;
