using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Funding.Events;

/// <summary>
/// <paramref name="Amount"/> is in the currency of the project (see <c>ProjectCreated</c>).
/// </summary>
public sealed record FundingAdded(Guid ProjectId, decimal Amount, DateOnly Date, string Reason) : DomainEvent;
