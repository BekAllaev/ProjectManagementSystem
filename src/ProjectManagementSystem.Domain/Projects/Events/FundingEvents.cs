using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record FundingAdded(Guid ProjectId, decimal Amount, string Currency, DateOnly Date, string Reason) : DomainEvent;
