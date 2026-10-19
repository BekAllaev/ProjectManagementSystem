using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Projects.Events;

public sealed record FundingAdded(Guid ProjectId, decimal Amount, string Currency, DateOnly Date, string Reason) : IDomainEvent;
