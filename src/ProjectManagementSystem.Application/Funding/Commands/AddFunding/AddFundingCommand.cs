using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Funding.Commands.AddFunding;

/// <summary>
/// Adds a tranche of funding to the project. <paramref name="Amount"/> is in the currency of the project.
/// </summary>
public sealed record AddFundingCommand(Guid ProjectId, decimal Amount, DateOnly Date, string? Reason) : ICommand;
