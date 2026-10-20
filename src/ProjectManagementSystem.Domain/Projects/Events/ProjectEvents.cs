using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record ProjectCreated(Guid ProjectId, string Title, string Currency, DateTime CreationDate) : DomainEvent;

public sealed record ProjectStarted(Guid ProjectId, DateTime StartDate) : DomainEvent;

public sealed record ProjectCompleted(Guid ProjectId, DateTime CompletionDate) : DomainEvent;
