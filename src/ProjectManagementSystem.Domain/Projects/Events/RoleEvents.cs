using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record RoleCreated(Guid ProjectId, Guid RoleId, string Name, string Description) : IDomainEvent;

public sealed record RoleRenamed(Guid ProjectId, Guid RoleId, string NewName) : IDomainEvent;

public sealed record RoleDescriptionChanged(Guid ProjectId, Guid RoleId, string Description) : IDomainEvent;

public sealed record RoleDeleted(Guid ProjectId, Guid RoleId) : IDomainEvent;
