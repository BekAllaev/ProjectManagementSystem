using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record TeamMemberAdded(
    Guid ProjectId,
    Guid TeamMemberId,
    Guid EmployeeId,
    Guid RoleId,
    DateOnly From,
    DateOnly? To,
    decimal HourlyRate) : DomainEvent;

public sealed record TeamMembershipEnded(Guid ProjectId, Guid TeamMemberId, Guid EmployeeId, DateOnly To) : DomainEvent;

public sealed record HourlyRateChanged(Guid ProjectId, Guid TeamMemberId, Guid EmployeeId, decimal Amount, DateOnly From) : DomainEvent;
