using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record TeamMemberAdded(
    Guid ProjectId,
    Guid TeamMemberId,
    Guid EmployeeId,
    Guid RoleId,
    DateOnly From,
    DateOnly? To) : IDomainEvent;

public sealed record TeamMembershipEnded(Guid ProjectId, Guid TeamMemberId, Guid EmployeeId, DateOnly To) : IDomainEvent;
