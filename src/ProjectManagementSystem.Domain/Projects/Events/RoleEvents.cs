using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record RoleCreated(Guid ProjectId, Guid RoleId, string Name, string Description) : DomainEvent;

public sealed record RoleRenamed(Guid ProjectId, Guid RoleId, string NewName) : DomainEvent;

public sealed record RoleDescriptionChanged(Guid ProjectId, Guid RoleId, string Description) : DomainEvent;

public sealed record RoleDeleted(Guid ProjectId, Guid RoleId) : DomainEvent;
